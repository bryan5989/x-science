using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	internal sealed class ScienceWindow {
		public ScienceWindow () {
			_logger = new Logger(this);
			_rect = new Rect(40, 40, 500, 400);
			_scrollPos = new Vector2();
			_filter = new ExperimentFilter();
			_progressTexture = new Texture2D(13, 13, TextureFormat.ARGB32, false);
			var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScienceChecklist.scienceProgress.png").ReadToEnd();
			_progressTexture.LoadImage(iconStream);
			_progressTexture.Apply();
			_completeTexture = new Texture2D(13, 13, TextureFormat.ARGB32, false);
			iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScienceChecklist.scienceComplete.png").ReadToEnd();
			_completeTexture.LoadImage(iconStream);
			_completeTexture.Apply();
			_emptyTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			_emptyTexture.SetPixels(new[] { Color.clear });
			_emptyTexture.Apply();
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
			_logger.Trace("RefreshScience");
			_filter.RefreshExperiments();
		}

		public void RefreshFilter () {
			_logger.Trace("RefreshFilter");
			_filter.UpdateFilter();
		}

		public void RecalculateSituation () {
			_logger.Trace("RecalculateSituation");
			var vessel = FlightGlobals.ActiveVessel;
			if (vessel == null) {
				if (_filter.CurrentSituation != null) {
					_filter.CurrentSituation = null;
				}
				return;
			}

			var body = vessel.mainBody;
			var situation = ScienceUtil.GetExperimentSituation(vessel);

			var biome = ScienceUtil.GetExperimentBiome(body, vessel.latitude, vessel.longitude);

			if (!string.IsNullOrEmpty(vessel.landedAt)) {
				biome = vessel.landedAt;
			}

			if (_filter.CurrentSituation != null && _filter.CurrentSituation.Biome == biome && _filter.CurrentSituation.ExperimentSituation == situation && _filter.CurrentSituation.Body == body) {
				return;
			}

			_filter.CurrentSituation = new Situation(body, situation, biome);
		}

		#endregion

		#region METHODS (PRIVATE)

		private void DrawControls (int windowId) {
			GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			var labelStyle = new GUIStyle(GUI.skin.label) {
				fontSize = 11,
				fontStyle = FontStyle.Italic,
			};

			var emptyLabelStyle = new GUIStyle(GUI.skin.label) {
				fontStyle = FontStyle.BoldAndItalic,
				alignment = TextAnchor.MiddleCenter,
				fontSize = 11,
				normal = {
					textColor = new Color(0.337f, 0.357f, 0.357f),
				},
			};

			var progressLabelStyle = new GUIStyle(emptyLabelStyle) {
				normal = {
					textColor = new Color(0.004f, 0.318f, 0.349f),
				},
			};

			var completeLabelStyle = new GUIStyle(progressLabelStyle) {
				normal = {
					textColor = new Color(0.035f, 0.420f, 0.114f),
				},
			};

			GUI.skin.horizontalScrollbarThumb.fixedHeight = 13;
			GUI.skin.horizontalScrollbar.fixedHeight = 13;
			var completePercent = _filter.DisplayExperiments.Count == 0 ? 1 : ((float) _filter.CompleteCount / (float) _filter.DisplayExperiments.Count);
			
			ProgressBar(
				new Rect (10, 27, 480, 13),
				_filter.DisplayExperiments.Count == 0 ? 1 : _filter.CompleteCount,
				_filter.DisplayExperiments.Count == 0 ? 1 : _filter.DisplayExperiments.Count,
				false,
				emptyLabelStyle,
				progressLabelStyle,
				completeLabelStyle);

			GUILayout.Space(20);

			GUILayout.BeginHorizontal();

			var oldPadding = GUI.skin.label.padding;
			GUI.skin.label.padding = new RectOffset(0, 0, 4, 0);
			GUILayout.Label(new GUIContent(string.Format("{0}/{1} complete.", _filter.CompleteCount, _filter.DisplayExperiments.Count)), GUILayout.Width(150));
			GUILayout.Label(new GUIContent("Search:"));
			GUI.skin.label.padding = oldPadding;

			_filter.Text = GUILayout.TextField(_filter.Text, GUILayout.ExpandWidth(true));

			if (GUILayout.Button(new GUIContent("x"), GUILayout.Width(25), GUILayout.Height(23))) {
				_filter.Text = string.Empty;
			}

			GUILayout.EndHorizontal();

			if (_filter.CurrentSituation != null) {
				GUILayout.Label(new GUIContent("Currently " + _filter.CurrentSituation.Description));
			}

			_scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.scrollView);
			var i = 0;

			foreach (var experiment in _filter.DisplayExperiments.Where(x => !_filter.HideComplete || !x.IsComplete)) {
				var rect = new Rect(5, 20 * i, 500, 20);
				DrawExperiment(experiment, rect, labelStyle, emptyLabelStyle, progressLabelStyle, completeLabelStyle);
				i++;
			}

			GUILayout.Space(20 * i);
			GUILayout.EndScrollView();
			
			GUILayout.BeginHorizontal();

			_filter.DisplayMode = (DisplayMode) GUILayout.SelectionGrid((int) _filter.DisplayMode, new[] {
				new GUIContent("On vessel"),
				new GUIContent("Unlocked"),
				new GUIContent("All"),
			}, 3);
			_filter.HideComplete = GUILayout.Toggle(_filter.HideComplete, new GUIContent("Hide complete"), GUILayout.ExpandWidth(true));

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		private void DrawExperiment (Experiment exp, Rect rect, GUIStyle labelStyle, GUIStyle emptyLabelStyle, GUIStyle progressLabelStyle, GUIStyle completeLabelStyle) {
			labelStyle.normal.textColor = exp.IsComplete ? Color.green : Color.yellow;
			var labelRect = new Rect(rect) {
				y = rect.y + 3,
			};
			var progressRect = new Rect(rect) {
				xMin = 395,
				xMax = 460,
				y = rect.y + 3,
			};

			GUI.Label(labelRect, new GUIContent(exp.Description), labelStyle);
			ProgressBar(progressRect, exp.CompletedScience, exp.TotalScience, true, emptyLabelStyle, progressLabelStyle, completeLabelStyle);
		}

		private void ProgressBar (Rect rect, float curr, float total, bool showValues, GUIStyle emptyLabelStyle, GUIStyle progressLabelStyle, GUIStyle completeLabelStyle) {
			GUI.skin.horizontalScrollbarThumb.normal.background = curr == 0
				? _emptyTexture
				: curr >= total ? _completeTexture : _progressTexture;
			var progressRect = new Rect(rect) {
				y = rect.y + 1,
			};
			GUI.HorizontalScrollbar(progressRect, 0, curr / total, 0, 1);

			if (showValues) {
				var labelRect = new Rect(rect) {
					y = rect.y - 1,
				};
				GUI.Label(labelRect, new GUIContent(string.Format("{0:0.#}  /  {1:0.#}", curr, total)), curr == 0 ? emptyLabelStyle : curr >= total ? completeLabelStyle : progressLabelStyle);
			}
		}

		#endregion

		#region FIELDS

		private Rect _rect;
		private Vector2 _scrollPos;

		private readonly Texture2D _progressTexture;
		private readonly Texture2D _completeTexture;
		private readonly Texture2D _emptyTexture;
		private readonly ExperimentFilter _filter;
		private readonly Logger _logger;
		private readonly int _windowId = UnityEngine.Random.Range(0, int.MaxValue);

		#endregion
	}
}
