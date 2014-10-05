using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScienceChecklist {
	internal sealed class ExperimentFilter {
		public ExperimentFilter () {
			_logger = new Logger(this);
			_displayMode = DisplayMode.Unlocked;
			_sortBy = SortOption.Experiment;
			_hideComplete = false;
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

		public SortOption SortBy {
			get {
				return _sortBy;
			}
			set {
				if (_sortBy != value) {
					_sortBy = value;
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

		public void RefreshExperiments () {
			if (ResearchAndDevelopment.Instance == null) {
				_allExperiments = new List<Experiment>();
				UpdateFilter();
				return;
			}

			var exps = new List<Experiment>();

			var experiments = ResearchAndDevelopment.GetExperimentIDs().Select(ResearchAndDevelopment.GetExperiment);
			var bodies = FlightGlobals.Bodies;
			var situations = Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>();
			var biomes = bodies.ToDictionary(x => x, ResearchAndDevelopment.GetBiomeTags);

			foreach (var experiment in experiments) {
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

						if ((situation == ExperimentSituations.FlyingHigh || situation == ExperimentSituations.FlyingLow) && !body.atmosphere) {
							// Some planets don't have an atmosphere for us to fly in.
							continue;
						}

						// TODO: This doesn't filter out impossible experiments based on the altitude of biomes.
						// e.g. Crew report while splashed down in the Highlands of Kerbin.

						if (!experiment.IsAvailableWhile(situation, body)) {
							// This experiment isn't valid for our current situation.
							continue;
						}

						if (experiment.BiomeIsRelevantWhile(situation)) {
							foreach (var biome in biomes[body]) {
								exps.Add(new Experiment(experiment, body, situation, biome));
							}
						} else {
							exps.Add(new Experiment(experiment, body, situation));
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
					//TODO: This
					query = query.Where(x => false);
					break;
				default:
					break;
			}

			switch (_sortBy) {
				case SortOption.Experiment:
					query = query.OrderBy(x => x.ScienceExperiment.experimentTitle);
					break;
				case SortOption.Body:
					query = query.OrderBy (x => x.Body.name).ThenBy(x => x.Biome);
					break;
				case SortOption.Science:
					query = query.OrderByDescending (x => x.TotalScience);
					break;
				default:
					break;
			}

			_displayExperiments = query.ToList();
			CompleteCount = _displayExperiments.Count(x => x.IsComplete);
		}

		private DisplayMode _displayMode;
		private SortOption _sortBy;
		private bool _hideComplete;

		private IList<Experiment> _allExperiments;
		private IList<Experiment> _displayExperiments;

		private readonly Logger _logger;
	}
}
