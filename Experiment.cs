using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScienceChecklist {
	internal sealed class Experiment {
		public Experiment (ScienceExperiment experiment, CelestialBody body, ExperimentSituations situation, string biome = null) {
			_experiment = experiment;
			_body = body;
			_situation = situation;
			_biome = biome;
		}

		#region PROPERTIES

		public ScienceExperiment    ScienceExperiment { get { return _experiment; } }
		public CelestialBody        Body              { get { return _body; } }
		public ExperimentSituations Situation         { get { return _situation; } }
		public string               Biome             { get { return _biome; } }

		#endregion

		#region METHODS (PUBLIC)

		public override string ToString () {
			return string.Format (
				"{0} while {1} {2}{3}",
				ScienceExperiment.experimentTitle,
				ToString(Situation),
				Body.theName,
				string.IsNullOrEmpty(Biome) ? string.Empty : string.Format ("'s {0}", Biome));
		}

		#endregion

		#region METHODS (PRIVATE)

		private string ToString (ExperimentSituations situation) {
			switch (situation) {
				case ExperimentSituations.FlyingHigh:
					return "flying high over";
				case ExperimentSituations.FlyingLow:
					return "flying low over";
				case ExperimentSituations.InSpaceHigh:
					return "in space high over";
				case ExperimentSituations.InSpaceLow:
					return "in space low over";
				case ExperimentSituations.SrfLanded:
					return "landed at";
				case ExperimentSituations.SrfSplashed:
					return "splashed down at";
				default:
					return situation.ToString();
			}
		}

		#endregion

		#region FIELDS

		private readonly ScienceExperiment _experiment;
		private readonly CelestialBody _body;
		private readonly ExperimentSituations _situation;
		private readonly string _biome;

		#endregion
	}
}
