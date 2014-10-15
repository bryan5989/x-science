﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScienceChecklist {
	internal sealed class Experiment {
		public Experiment (ScienceExperiment experiment, ScienceSubject subject, Situation situation, bool usesSubBiomes) {
			_experiment = experiment;
			_subject = subject;
			_situation = situation;
			_usesSubBiomes = usesSubBiomes;
			Update();
		}

		#region PROPERTIES

		public ScienceExperiment    ScienceExperiment { get { return _experiment; } }
		public ScienceSubject       ScienceSubject    { get { return _subject; } }
		public Situation            Situation         { get { return _situation; } }
		public bool                 UsesSubBiomes     { get { return _usesSubBiomes; } }

		public float  CompletedScience { get; private set; }
		public bool   IsUnlocked       { get; private set; }
		public float  TotalScience     { get; private set; }
		public bool   IsComplete       { get; private set; }

		public string Description {
			get {
				return string.Format(
					"{0} while {1}",
					ScienceExperiment.experimentTitle,
					Situation.Description);
			}
		}

		#endregion

		#region METHODS (PUBLIC)

		public void Update () {
			IsUnlocked = ScienceExperiment.id == "evaReport" ||
				ScienceExperiment.id == "surfaceSample" ||
				ScienceExperiment.id == "crewReport" ||
				PartLoader.Instance.parts.Any(x => ResearchAndDevelopment.PartModelPurchased(x) && x.partPrefab.Modules != null && x.partPrefab.Modules.OfType<ModuleScienceExperiment>().Any(y => y.experimentID == ScienceExperiment.id));

			CompletedScience = _subject.science * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
			TotalScience = _subject.scienceCap * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
			IsComplete = CompletedScience > TotalScience || TotalScience - CompletedScience < 0.1;
		}

		#endregion

		#region FIELDS

		private readonly ScienceExperiment _experiment;
		private readonly ScienceSubject _subject;
		private readonly Situation _situation;
		private readonly bool _usesSubBiomes;

		#endregion
	}
}
