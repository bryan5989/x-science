﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScienceChecklist {
	/// <summary>
	/// An object that represents a ScienceExperiement in a given situation.
	/// </summary>
	internal sealed class Experiment {
		/// <summary>
		/// Creates a new instance of the Experiment class.
		/// </summary>
		/// <param name="experiment">The ScienceExperiment to be used.</param>
		/// <param name="situation">The Situation this experiment is valid in.</param>
		/// <param name="usesSubBiomes">Indicates whether this experiment uses the special KSC biomes.</param>
		/// <param name="onboardScience">A collection of all onboard ScienceData.</param>
		public Experiment (ScienceExperiment experiment, Situation situation, bool usesSubBiomes, IEnumerable<ScienceData> onboardScience) {
			_experiment = experiment;
			_situation = situation;
			_usesSubBiomes = usesSubBiomes;
			Update(onboardScience);
		}

		#region PROPERTIES

		/// <summary>
		/// Gets the ScienceExperiment used by ResearchAndDevelopment.
		/// </summary>
		public ScienceExperiment    ScienceExperiment { get { return _experiment; } }
		/// <summary>
		/// Gets the Situation in which this experiment is valid.
		/// </summary>
		public Situation            Situation         { get { return _situation; } }
		/// <summary>
		/// Gets a value indicating whether this experiment uses the special KSC biomes.
		/// </summary>
		public bool                 UsesSubBiomes     { get { return _usesSubBiomes; } }
		
		/// <summary>
		/// Gets the ResearchAndDevelopment ID for this experiment.
		/// </summary>
		public string Id {
			get {
				return string.Format("{0}@{1}{2}{3}", ScienceExperiment.id, Situation.Body.name, Situation.ExperimentSituation, Situation.SubBiome ?? Situation.Biome ?? string.Empty);
			}
		}

		/// <summary>
		/// Gets the amount of science completed for this experiment.
		/// </summary>
		public float  CompletedScience { get; private set; }
		/// <summary>
		/// Gets a value indicating if this experiment has been unlocked in the tech tree.
		/// </summary>
		public bool   IsUnlocked       { get; private set; }
		/// <summary>
		/// Gets the total amount of science available for this experiment.
		/// </summary>
		public float  TotalScience     { get; private set; }
		/// <summary>
		/// Gets a value indicating whether all the science has been obtained for this experiment.
		/// </summary>
		public bool   IsComplete       { get; private set; }
		/// <summary>
		/// Gets the amount of science for this experiment that is currently stored on vessels.
		/// </summary>
		public float OnboardScience    { get; private set; }
		/// <summary>
		/// Gets the ScienceSubject containing information on how much science has been retrieved from this experiment.
		/// </summary>
		public ScienceSubject ScienceSubject { get; private set; }

		/// <summary>
		/// Gets the human-readable description of this experiment.
		/// </summary>
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

		/// <summary>
		/// Updates the IsUnlocked, CompletedScience, TotalScience, OnboardScience, and IsComplete fields.
		/// </summary>
		/// <param name="onboardScience">The total onboard ScienceData.</param>
		public void Update (IEnumerable<ScienceData> onboardScience) {
			ScienceSubject = (ResearchAndDevelopment.GetSubjects() ?? new List<ScienceSubject> ())
				.SingleOrDefault(x => x.id == Id)
				?? new ScienceSubject(ScienceExperiment, Situation.ExperimentSituation, Situation.Body, Situation.SubBiome ?? Situation.Biome ?? string.Empty);
			IsUnlocked = ScienceExperiment.id == "evaReport" ||
				ScienceExperiment.id == "surfaceSample" ||
				ScienceExperiment.id == "crewReport" ||
				PartLoader.Instance.parts.Any(x => ResearchAndDevelopment.PartModelPurchased(x) && x.partPrefab.Modules != null && x.partPrefab.Modules.OfType<ModuleScienceExperiment>().Any(y => y.experimentID == ScienceExperiment.id));

			CompletedScience = ScienceSubject.science * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
			TotalScience = ScienceSubject.scienceCap * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
			IsComplete = CompletedScience > TotalScience || TotalScience - CompletedScience < 0.1;

			var multiplier = ScienceExperiment.baseValue / ScienceExperiment.scienceCap;
			
			var data = onboardScience
				.Where (x => x.subjectID == ScienceSubject.id)
				.ToList ();
			
			OnboardScience = 0;
			foreach (var i in data) {
				var next = (TotalScience - (CompletedScience + OnboardScience)) * multiplier;
				OnboardScience += next;
			}
		}

		#endregion

		#region FIELDS

		private readonly ScienceExperiment _experiment;
		private readonly Situation _situation;
		private readonly bool _usesSubBiomes;

		#endregion
	}
}
