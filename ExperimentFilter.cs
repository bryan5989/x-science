using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ScienceChecklist {
	internal sealed class ExperimentFilter {
		public ExperimentFilter () {
			_logger = new Logger(this);
			_displayMode = DisplayMode.Unlocked;
			_hideComplete = false;
			_text = string.Empty;
		}

		public IList<Experiment> AllExperiments     { get { return _allExperiments; } }
		public IList<Experiment> DisplayExperiments { get { return _displayExperiments; } }
		public int               CompleteCount      { get; private set; }

		public DisplayMode DisplayMode {
			get {
				return _displayMode;
			} set {
				if (_displayMode != value) {
					_displayMode = value;
					UpdateFilter();
				}
			}
		}

		public bool HideComplete {
			get {
				return _hideComplete;
			}
			set {
				if (_hideComplete != value) {
					_hideComplete = value;
					UpdateFilter();
				}
			}
		}

		public string Text {
			get {
				return _text;
			}
			set {
				if (_text != value) {
					_text = value;
					UpdateFilter();
				}
			}
		}

		public void RefreshExperiments () {
			_logger.Trace("RefreshExperiments");
			if (ResearchAndDevelopment.Instance == null) {
				_logger.Debug("ResearchAndDevelopment not instantiated.");
				_allExperiments = new List<Experiment>();
				UpdateFilter();
				return;
			}

			var exps = new List<Experiment>();

			var experiments = PartLoader.Instance.parts
				.SelectMany(x => x.partPrefab.FindModulesImplementing<ModuleScienceExperiment>())
				.Select(x => new {
					Module = x,
					Experiment = ResearchAndDevelopment.GetExperiment(x.experimentID),
				})
				.GroupBy(x => x.Experiment)
				.ToDictionary(x => x.Key, x => x.First().Module);

			experiments[ResearchAndDevelopment.GetExperiment("evaReport")] = null;
			experiments[ResearchAndDevelopment.GetExperiment("surfaceSample")] = null;

			var bodies = FlightGlobals.Bodies;
			var situations = Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>();
			var biomes = bodies.ToDictionary(x => x, ResearchAndDevelopment.GetBiomeTags);
			var subjects = ResearchAndDevelopment.GetSubjects();

			foreach (var experiment in experiments.Keys) {
				
				var sitMask = experiment.situationMask;
				var biomeMask = experiment.biomeMask;
				if (sitMask == 0 && experiments[experiment] != null) {
					// OrbitalScience support
					var sitMaskField = experiments[experiment].GetType().GetField("sitMask");
					if (sitMaskField != null) {
						sitMask = (uint) (int) sitMaskField.GetValue(experiments[experiment]);
						_logger.Debug("Setting sitMask to " + sitMask + " for " + experiment.experimentTitle);
					}

					if (biomeMask == 0) {
						var biomeMaskField = experiments[experiment].GetType().GetField("bioMask");
						if (biomeMaskField != null) {
							biomeMask = (uint) (int) biomeMaskField.GetValue(experiments[experiment]);
							_logger.Debug("Setting biomeMask to " + biomeMask + " for " + experiment.experimentTitle);
						}
					}
				}

				foreach (var body in bodies) {
					if (experiment.requireAtmosphere && !body.atmosphere) {
						// If the whole planet doesn't have an atmosphere, then there's not much point continuing.
						continue;
					}

					foreach (var situation in situations) {
						if (situation == ExperimentSituations.SrfSplashed && !body.ocean) {
							// Some planets don't have an ocean for us to be splashed down in.
							continue;
						}

						if (situation == ExperimentSituations.SrfLanded && (body.name == "Jool" || body.name == "Sun")) {
							// Jool and the Sun don't have a surface.
							continue;
						}

						if ((situation == ExperimentSituations.FlyingHigh || situation == ExperimentSituations.FlyingLow) && !body.atmosphere) {
							// Some planets don't have an atmosphere for us to fly in.
							continue;
						}

						// TODO: This doesn't filter out impossible experiments based on the altitude of biomes.
						// e.g. Crew report while splashed down in the Highlands of Kerbin.

						if ((sitMask & (uint) situation) == 0) {
							// This experiment isn't valid for our current situation.
							continue;
						}

						if (biomes[body].Any() && (biomeMask & (uint) situation) != 0) {
							foreach (var biome in biomes[body]) {
								var subject = subjects
									.Where (x => x.id == GetId(experiment, body, situation, biome))
									.SingleOrDefault () ?? new ScienceSubject(experiment, situation, body, biome);

								exps.Add(new Experiment(experiment, subject, body, situation, biome));
							}
						} else {
							var subject = subjects
								.Where(x => x.id == GetId(experiment, body, situation))
								.SingleOrDefault() ?? new ScienceSubject(experiment, situation, body);
							exps.Add(new Experiment(experiment, subject, body, situation));
						}
					}
				}
			}

			_allExperiments = exps;
			UpdateFilter();
		}

		public void UpdateFilter () {
			_logger.Trace("UpdateFilter");
			var query = _allExperiments.AsEnumerable();
			switch (_displayMode) {
				case DisplayMode.All:
					break;
				case DisplayMode.Unlocked:
					query = query.Where(x => x.IsUnlocked == true);
					break;
				case DisplayMode.ActiveVessel:
					query = ApplyActiveVesselFilter(query);
					break;
				default:
					break;
			}

			query = query.OrderBy(x => x.TotalScience);

			var search = Text.Split(' ').Select (x => x.Split('|')).ToList ();

			query = query.Where(x => string.IsNullOrEmpty(Text) ||
				search.All(y => y.Any(z => x.Description.ToLowerInvariant().Contains(z.ToLowerInvariant()))));

			CompleteCount = query.Count(x => x.IsComplete);
			_displayExperiments = query.ToList();
		}

		private string GetId (ScienceExperiment exp, CelestialBody body, ExperimentSituations sit, string biome = null) {
			return string.Format("{0}@{1}{2}{3}", exp.id, body.name, sit, biome ?? string.Empty);
		}

		private IEnumerable<Experiment> ApplyActiveVesselFilter (IEnumerable<Experiment> src) {
			_logger.Trace("ApplyActiveVesselFilter");
			switch (HighLogic.LoadedScene) {
				case GameScenes.FLIGHT:
					var vessel = FlightGlobals.ActiveVessel;
					return vessel == null
						? Enumerable.Empty<Experiment> ()
						: ApplyPartFilter(src, vessel.FindPartModulesImplementing<ModuleScienceExperiment>(), vessel.GetCrewCount() > 0);
				case GameScenes.EDITOR:
				case GameScenes.SPH:
					return EditorLogic.startPod == null || EditorLogic.SortedShipList == null
						? Enumerable.Empty<Experiment> ()
						: ApplyPartFilter(src, EditorLogic.SortedShipList.SelectMany(x => x.Modules.OfType<ModuleScienceExperiment> ()), EditorLogic.SortedShipList.Any (x => x != null && x.CrewCapacity > 0));
				case GameScenes.CREDITS:
				case GameScenes.LOADING:
				case GameScenes.LOADINGBUFFER:
				case GameScenes.MAINMENU:
				case GameScenes.PSYSTEM:
				case GameScenes.SETTINGS:
				case GameScenes.SPACECENTER:
				case GameScenes.TRACKSTATION:
				default:
					// No active vessel for these scences.
					return Enumerable.Empty<Experiment> ();
			}
		}

		private IEnumerable<Experiment> ApplyPartFilter (IEnumerable<Experiment> src, IEnumerable<ModuleScienceExperiment> modules, bool hasCrew) {
			_logger.Trace("ApplyPartFilter");
			var experiments = modules
				.Select(x => x.experimentID)
				.Distinct();
			return src.Where(x =>
				(x.ScienceExperiment.id != "crewReport" && experiments.Contains(x.ScienceExperiment.id)) || // unmanned - crewReport needs to be explicitly ignored as we need crew for that experiment even though it's a module on the capsules
				(hasCrew && (x.ScienceExperiment.id == "crewReport" || x.ScienceExperiment.id == "surfaceSample" || x.ScienceExperiment.id == "evaReport"))); // manned
		}

		private DisplayMode _displayMode;
		private bool _hideComplete;
		private string _text;

		private IList<Experiment> _allExperiments;
		private IList<Experiment> _displayExperiments;

		private readonly Logger _logger;
	}
}
