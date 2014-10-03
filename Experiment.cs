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

			Update();
		}

		#region PROPERTIES

		public ScienceExperiment    ScienceExperiment { get { return _experiment; } }
		public CelestialBody        Body              { get { return _body; } }
		public ExperimentSituations Situation         { get { return _situation; } }
		public string               Biome             { get { return _biome; } }

		public float CompletedScience { get; private set; }
		public bool  IsUnlocked       { get; private set; }

		public float TotalScience { get { return ScienceExperiment.scienceCap * ScienceModifier; } }
		public bool  IsComplete   { get { return Math.Abs (CompletedScience - TotalScience) < 0.01; } }
		public string Description {
			get {
				return string.Format(
					"{0} while {1} {2}{3}",
					ScienceExperiment.experimentTitle,
					ToString(Situation),
					Body.theName,
					string.IsNullOrEmpty(Biome) ? string.Empty : string.Format("'s {0}", Biome));
			}
		}

		public float ScienceModifier {
			get {
				switch (Situation) {
					case ExperimentSituations.FlyingHigh:
						return Body.scienceValues.FlyingHighDataValue;
					case ExperimentSituations.FlyingLow:
						return Body.scienceValues.FlyingLowDataValue;
					case ExperimentSituations.InSpaceHigh:
						return Body.scienceValues.InSpaceHighDataValue;
					case ExperimentSituations.InSpaceLow:
						return Body.scienceValues.InSpaceLowDataValue;
					case ExperimentSituations.SrfLanded:
						return Body.scienceValues.LandedDataValue;
					case ExperimentSituations.SrfSplashed:
						return Body.scienceValues.SplashedDataValue;
					default:
						return 1;
				}
			}
		}

		#endregion

		#region METHODS (PUBLIC)

		public void Update () {
			var subject = ResearchAndDevelopment.GetSubjects().SingleOrDefault(x => x.id == ScienceExperiment.id + "@" + Body.name + Situation + Biome);

			IsUnlocked = ScienceExperiment.id == "evaReport" ||
				ScienceExperiment.id == "surfaceSample" ||
				ScienceExperiment.id == "crewReport" ||
				PartLoader.Instance.parts.Any(x => ResearchAndDevelopment.PartModelPurchased(x) && x.partPrefab.Modules != null && x.partPrefab.Modules.OfType<ModuleScienceExperiment>().Any(y => y.experimentID == ScienceExperiment.id));
			
			CompletedScience = subject == null ? 0 : subject.science;
		}

		public override string ToString () {
			return string.Format (
				"{6} {0} while {1} {2}{3} for {4:0.0} / {5:0.0} science",
				ScienceExperiment.experimentTitle,
				ToString(Situation),
				Body.theName,
				string.IsNullOrEmpty(Biome) ? string.Empty : string.Format("'s {0}", Biome),
				CompletedScience,
				TotalScience,
				IsComplete ? "[x]" : "[ ]");
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
