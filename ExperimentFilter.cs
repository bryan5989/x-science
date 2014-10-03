using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScienceChecklist {
	internal sealed class ExperimentFilter {
		public ExperimentFilter () {
			_logger = new Logger(this);
			_showLocked = false;
		}

		public IList<Experiment> AllExperiments     { get { return _allExperiments; } }
		public IList<Experiment> DisplayExperiments { get { return _displayExperiments; } }
		public int               CompleteCount      { get; private set; }

		public bool ShowLockedExperiments {
			get {
				return _showLocked;
			} set {
				if (_showLocked != value) {
					_showLocked = value;
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
			CompleteCount = _allExperiments.Count(x => x.IsComplete);
			UpdateFilter();
		}

		public void UpdateFilter () {
			_logger.Trace("UpdateFilter");
			var query = _allExperiments.AsEnumerable();

			/*if (_showComplete != null) {
				query = query.Where(x => x.IsComplete == _showComplete.Value);
			}*/

			if (_showLocked == false) {
				query = query.Where(x => x.IsUnlocked == true);
			}

			_displayExperiments = query
				.OrderByDescending(x => x.TotalScience - x.CompletedScience)
				.ToList();
		}

		private bool _showLocked;

		private IList<Experiment> _allExperiments;
		private IList<Experiment> _displayExperiments;

		private readonly Logger _logger;
	}
}
