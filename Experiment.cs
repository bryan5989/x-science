using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScienceChecklist {
	internal sealed class Experiment {
		public Experiment (ScienceExperiment experiment, ScienceSubject subject, CelestialBody body, ExperimentSituations situation, string biome = null) {
			_experiment = experiment;
			_subject = subject;
			_body = body;
			_situation = situation;
			_biome = biome;
			FormattedBiome = BiomeToString(biome);
			Update();
		}

		#region PROPERTIES

		public ScienceExperiment    ScienceExperiment { get { return _experiment; } }
		public ScienceSubject       ScienceSubject    { get { return _subject; } }
		public CelestialBody        Body              { get { return _body; } }
		public ExperimentSituations Situation         { get { return _situation; } }
		public string               Biome             { get { return _biome; } }

		public string FormattedBiome   { get; private set; }
		public float  CompletedScience { get; private set; }
		public bool   IsUnlocked       { get; private set; }
		public float  TotalScience     { get; private set; }
		public bool   IsComplete       { get; private set; }

		public string Description {
			get {
				return string.Format(
					"{0} while {1} {2}{3}",
					ScienceExperiment.experimentTitle,
					ToString(Situation),
					Body.theName,
					string.IsNullOrEmpty(FormattedBiome) ? string.Empty : string.Format("'s {0}", FormattedBiome));
			}
		}

		public float ScienceModifier {
			get {
				switch (Situation) {
					case ExperimentSituations.FlyingHigh:
						return Body.scienceValues.FlyingLowDataValue;
					case ExperimentSituations.FlyingLow:
						return Body.scienceValues.FlyingLowDataValue;
					case ExperimentSituations.InSpaceHigh:
						return Body.scienceValues.InSpaceLowDataValue;
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
			IsUnlocked = ScienceExperiment.id == "evaReport" ||
				ScienceExperiment.id == "surfaceSample" ||
				ScienceExperiment.id == "crewReport" ||
				PartLoader.Instance.parts.Any(x => ResearchAndDevelopment.PartModelPurchased(x) && x.partPrefab.Modules != null && x.partPrefab.Modules.OfType<ModuleScienceExperiment>().Any(y => y.experimentID == ScienceExperiment.id));

			CompletedScience = _subject.science;
			TotalScience = _subject.scienceCap * Body.scienceValues.RecoveryValue;
			//TotalScience = ScienceExperiment.baseValue * ScienceModifier;
			IsComplete = Math.Abs(CompletedScience - TotalScience) < 0.01;
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
					return "in space near";
				case ExperimentSituations.SrfLanded:
					return "landed at";
				case ExperimentSituations.SrfSplashed:
					return "splashed down at";
				default:
					return situation.ToString();
			}
		}

		private string BiomeToString (string biome) {
			return Regex.Replace(biome ?? string.Empty, "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1").Trim();
		}

		#endregion

		#region FIELDS

		private readonly ScienceExperiment _experiment;
		private readonly ScienceSubject _subject;
		private readonly CelestialBody _body;
		private readonly ExperimentSituations _situation;
		private readonly string _biome;

		#endregion
	}
}
