using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	internal sealed class ScienceWindow {

		public ScienceWindow () {
			_logger = new Logger(this);
			_rect = new Rect(0, 0, 400, 400);
			_scrollPos = new Vector2();
		}

		#region PROPERTIES

		public bool IsVisible { get; set; }

		#endregion

		#region METHODS (PUBLIC)

		public void Draw () {
			if (!IsVisible) {
				return;
			}

			var oldSkin = GUI.skin;
			GUI.skin = GameObject.Instantiate(HighLogic.Skin) as GUISkin;

			_rect = GUILayout.Window(_windowId, _rect, DrawControls, "[x] Science!");
			GUI.skin = oldSkin;
		}

		public void RefreshScience () {
			if (ResearchAndDevelopment.Instance == null) {
				_experiments = new List<Experiment>();
			}

			var exps = new List<Experiment>();

			var experiments = ResearchAndDevelopment.GetExperimentIDs().Select(ResearchAndDevelopment.GetExperiment);
			var bodies = FlightGlobals.Bodies;
			var situations = Enum.GetValues (typeof (ExperimentSituations)).Cast<ExperimentSituations>();
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

			_experiments = exps;
			_completeCount = _experiments.Count(x => x.IsComplete);
			_availableCount = _experiments.Count(x => x.IsAvailable);

			_logger.Info("Found " + _experiments.Count + " sciences");
		}

		#endregion

		#region METHODS (PRIVATE)
		
		private void DrawControls (int windowId) {
			GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			GUILayout.Label(new GUIContent(string.Format("{0}/{1}({2}) experiments complete.", _completeCount, _availableCount, _experiments.Count)));
			_scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.scrollView);

			foreach (var experiment in _experiments) {
				GUILayout.Label(new GUIContent(experiment.ToString ()), GUILayout.ExpandWidth(true));
			}

			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		#endregion

		#region FIELDS

		private Logger _logger;
		private Rect _rect;
		private Vector2 _scrollPos;
		private List<Experiment> _experiments;
		private int _completeCount;
		private int _availableCount;

		private readonly int _windowId = UnityEngine.Random.Range(0, int.MaxValue);

		#endregion
	}
}
