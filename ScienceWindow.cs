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
			_filter.RefreshExperiments();
		}

		public void RefreshFilter () {
			_filter.UpdateFilter();
		}

		#endregion

		#region METHODS (PRIVATE)
		
		private void DrawControls (int windowId) {
			GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			
			GUI.skin.horizontalScrollbarThumb.fixedHeight = 13;
			GUI.skin.horizontalScrollbar.fixedHeight = 13;
			var completePercent = _filter.DisplayExperiments.Count == 0 ? 1 : ((float) _filter.CompleteCount / (float) _filter.DisplayExperiments.Count);
			ProgressBar(
				_filter.DisplayExperiments.Count == 0 ? 1 : _filter.CompleteCount,
				_filter.DisplayExperiments.Count == 0 ? 1 : _filter.DisplayExperiments.Count,
				false,
				GUILayout.ExpandWidth(true),
				GUILayout.Height(13));

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

			GUILayout.BeginHorizontal();

			_scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.scrollView);

			foreach (var experiment in _filter.DisplayExperiments.Where (x => !_filter.HideComplete || !x.IsComplete)) {
				DrawExperiment(experiment);
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndScrollView();
			GUILayout.EndHorizontal();

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

		private void DrawExperiment (Experiment exp) {
			GUI.skin.label.fontSize = 11;
			GUI.skin.label.fontStyle = FontStyle.Italic;
			GUI.skin.label.normal.textColor = exp.IsComplete ? Color.green : Color.yellow;
			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.MaxHeight(10));
			GUILayout.Label(new GUIContent(exp.Description), GUILayout.Height(7));
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();
			GUILayout.Space(6);

			ProgressBar(exp.CompletedScience, exp.TotalScience, true, GUILayout.Width(75), GUILayout.Height(7));
			GUILayout.EndVertical();
			GUILayout.Space(4);
			GUILayout.EndHorizontal();
		}

		private void ProgressBar (float curr, float total, bool showValues, params GUILayoutOption[] options) {
			GUI.skin.horizontalScrollbarThumb.normal.background = curr == 0 ? _emptyTexture : _progressTexture;
			GUILayout.HorizontalScrollbar(0, curr / total, 0, 1, options);

			if (showValues) {
				GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

				var style = new GUIStyle(GUI.skin.label);
				style.fontStyle = FontStyle.BoldAndItalic;
				style.normal.textColor = new Color(0.368f, 0.368f, 0.368f);
				style.alignment = TextAnchor.MiddleCenter;
				style.fixedHeight = 0;
				style.fixedWidth = 75;
				style.contentOffset = new Vector2(0, -5);

				GUILayout.Label(new GUIContent(string.Format("{0:0.#}  /  {1:0.#}", curr, total)), style, GUILayout.Height(0.1f));
				
				GUILayout.EndHorizontal();
			}
		}

		#endregion

		#region FIELDS

		private Rect _rect;
		private Vector2 _scrollPos;

		private Texture2D _progressTexture;
		private readonly Texture2D _emptyTexture;
		private readonly ExperimentFilter _filter;
		private readonly Logger _logger;
		private readonly int _windowId = UnityEngine.Random.Range(0, int.MaxValue);

		#endregion
	}
}
