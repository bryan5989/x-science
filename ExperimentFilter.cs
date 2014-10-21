﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	/// <summary>
	/// Stores a cache of experiments available in the game, and provides methods to manipulate a filtered view of this collection.
	/// </summary>
	internal sealed class ExperimentFilter {
		/// <summary>
		/// Creates a new instance of the ExperimentFilter class.
		/// </summary>
		public ExperimentFilter () {
			_logger = new Logger(this);
			_displayMode = DisplayMode.Unlocked;
			_hideComplete = false;
			_text = string.Empty;
			_kscBiomes = new List<string>();
			AllExperiments = new List<Experiment>();
		}

		/// <summary>
		/// Gets all experiments that are available in the game.
		/// </summary>
		public IList<Experiment> AllExperiments     { get; private set; }
		/// <summary>
		/// Gets the experiments that should currently be displayed in the experiment list.
		/// </summary>
		public IList<Experiment> DisplayExperiments { get; private set; }
		/// <summary>
		/// Gets the number of display experiments that are complete.
		/// </summary>
		public int               CompleteCount      { get; private set; }
		/// <summary>
		/// Gets the total number of display experiments.
		/// </summary>
		public int               TotalCount         { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating the current mode of the filter.
		/// </summary>
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

		/// <summary>
		/// Gets or sets a value indicating whether to hide completed experiments.
		/// </summary>
		public bool HideComplete {
			get {
				return _hideComplete;
			} set {
				if (_hideComplete != value) {
					_hideComplete = value;
					UpdateFilter();
				}
			}
		}

		/// <summary>
		/// Gets or sets a string to be used for filtering experiments.
		/// </summary>
		public string Text {
			get {
				return _text;
			} set {
				if (_text != value) {
					_text = value;
					UpdateFilter();
				}
			}
		}

		/// <summary>
		/// Gets or sets the current situation.
		/// </summary>
		public Situation CurrentSituation {
			get {
				return _situation;
			} set {
				if (_situation != value) {
					_situation = value;
					UpdateFilter();
				}
			}
		}

		/// <summary>
		/// Calls the Update method on all experiments.
		/// </summary>
		public void UpdateExperiments () {
			_logger.Trace("UpdateExperiments");
			var onboardScience = GameHelper.GetOnboardScience();

			foreach (var exp in AllExperiments) {
				exp.Update(onboardScience);
			}
		}

		/// <summary>
		/// Refreshes the experiment cache. THIS IS VERY EXPENSIVE.
		/// </summary>
		public void RefreshExperimentCache () {
			_logger.Info("RefreshExperimentCache");
			if (ResearchAndDevelopment.Instance == null) {
				_logger.Debug("ResearchAndDevelopment not instantiated.");
				AllExperiments = new List<Experiment>();
				UpdateFilter();
				return;
			}

			if (PartLoader.Instance == null) {
				_logger.Debug("PartLoader not instantiated.");
				AllExperiments = new List<Experiment>();
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
			var biomes = bodies.ToDictionary(x => x, x => x.BiomeMap.Attributes.Select(y => y.name).ToArray());

			_kscBiomes = _kscBiomes.Any () ? _kscBiomes : UnityEngine.Object.FindObjectsOfType<Collider>()
				.Where(x => x.gameObject.layer == 15)
				.Select(x => x.gameObject.tag)
				.Where(x => x != "Untagged")
				.Where(x => !x.Contains("KSC_Runway_Light"))
				.Where(x => !x.Contains("KSC_Pad_Flag_Pole"))
				.Where(x => !x.Contains("Ladder"))
				.Select(x => Vessel.GetLandedAtString(x))
				.Select(x => x.Replace(" ", ""))
				.Distinct()
				.ToList();

			var subjects = ResearchAndDevelopment.GetSubjects();

			var onboardScience = GameHelper.GetOnboardScience();

			foreach (var experiment in experiments.Keys) {
				var useSubBiomes = true;
				var sitMask = experiment.situationMask;
				var biomeMask = experiment.biomeMask;
				if (sitMask == 0 && experiments[experiment] != null) {
					// OrbitalScience support
					useSubBiomes = false;
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
								exps.Add(new Experiment(experiment, new Situation(body, situation, biome), useSubBiomes, onboardScience));
							}

							if ((body.name == "Kerbin") && situation == ExperimentSituations.SrfLanded && useSubBiomes) {
								foreach (var kscBiome in _kscBiomes) {
									// Ew.
									exps.Add(new Experiment(experiment, new Situation(body, situation, "Shores", kscBiome), useSubBiomes, onboardScience));
								}
							}

						} else {
							exps.Add(new Experiment(experiment, new Situation(body, situation), useSubBiomes, onboardScience));
						}
					}
				}
			}

			AllExperiments = exps;
			UpdateFilter();
		}

		/// <summary>
		/// Recalculates the experiments to be displayed.
		/// </summary>
		public void UpdateFilter () {
			_logger.Trace("UpdateFilter");
			var query = AllExperiments.AsEnumerable();
			switch (_displayMode) {
				case DisplayMode.All:
					break;
				case DisplayMode.Unlocked:
					query = query.Where(x => x.IsUnlocked == true);
					break;
				case DisplayMode.ActiveVessel:
					query = ApplyActiveVesselFilter(query);
					break;
				case DisplayMode.CurrentSituation:
					query = ApplyCurrentSituationFilter(query);
					break;
				default:
					break;
			}

			foreach (var word in Text.Split(' ')) {
				var options = word.Split('|');
				query = query.Where(x => options.Any(o => {
					var s = o;
					var negate = false;
					if (o.StartsWith("-", StringComparison.InvariantCultureIgnoreCase)) {
						negate = true;
						s = o.Substring(1);
					}

					return x.Description.ToLowerInvariant().Contains(s.ToLowerInvariant()) == !negate;
				}));
			}

			query = query.OrderBy (x => x.TotalScience);

			CompleteCount = query.Count(x => x.IsComplete);
			TotalCount = query.Count();
			DisplayExperiments = query.Where (x => !HideComplete || !x.IsComplete).ToList();
		}

		/// <summary>
		/// Filters a collection of experiments to only return ones that can be performed on the current vessel.
		/// </summary>
		/// <param name="src">The source experiment collection.</param>
		/// <returns>A filtered collection of experiments that can be performed on the current vessel.</returns>
		private IEnumerable<Experiment> ApplyActiveVesselFilter (IEnumerable<Experiment> src) {
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
					// No active vessel for these scenes.
					return Enumerable.Empty<Experiment> ();
			}
		}

		/// <summary>
		/// Filters a collection of experiments to only return ones that can be performed on a vessel made from the given modules.
		/// </summary>
		/// <param name="src">The source experiment collection.</param>
		/// <param name="modules">The available modules.</param>
		/// <param name="hasCrew">A flag indicating whether the modules currently have crew onboard.</param>
		/// <returns>A filtered collection of experiments that can be performed on a vessel made from the given modules.</returns>
		private IEnumerable<Experiment> ApplyPartFilter (IEnumerable<Experiment> src, IEnumerable<ModuleScienceExperiment> modules, bool hasCrew) {
			var experiments = modules
				.Select(x => x.experimentID)
				.Distinct();
			return src.Where(x =>
				(x.ScienceExperiment.id != "crewReport" && experiments.Contains(x.ScienceExperiment.id)) || // unmanned - crewReport needs to be explicitly ignored as we need crew for that experiment even though it's a module on the capsules.
				(hasCrew && experiments.Contains("crewReport") && x.ScienceExperiment.id == "crewReport") || // manned crewReport
				(hasCrew && (x.ScienceExperiment.id == "surfaceSample" || x.ScienceExperiment.id == "evaReport"))); // manned
		}

		/// <summary>
		/// Filters a collection of experiments to only return ones that can be performed in the current situation.
		/// </summary>
		/// <param name="src">The source experiment collection</param>
		/// <returns>A filtered collection of experiments that can be performed in the current situation.</returns>
		private IEnumerable<Experiment> ApplyCurrentSituationFilter (IEnumerable<Experiment> src) {
			if (HighLogic.LoadedScene != GameScenes.FLIGHT || CurrentSituation == null) {
				return Enumerable.Empty<Experiment>();
			}

			var vessel = FlightGlobals.ActiveVessel;
			if (vessel == null) {
				return Enumerable.Empty<Experiment>();
			}

			src = ApplyActiveVesselFilter(src);

			return src
				.Where(x => x.Situation.Body == CurrentSituation.Body)
				.Where(x => string.IsNullOrEmpty(x.Situation.Biome) || x.Situation.Biome == CurrentSituation.Biome)
				.Where(x => x.UsesSubBiomes ? x.Situation.SubBiome == CurrentSituation.SubBiome : true)
				.Where(x => x.Situation.ExperimentSituation == CurrentSituation.ExperimentSituation);
		}

		private DisplayMode _displayMode;
		private bool _hideComplete;
		private string _text;
		private Situation _situation;

		private IList<string> _kscBiomes;

		private readonly Logger _logger;
	}
}
