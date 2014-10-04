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
			_rect = new Rect(0, 0, 600, 500);
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

		#endregion

		#region METHODS (PRIVATE)
		
		private void DrawControls (int windowId) {
			GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			GUILayout.Label(new GUIContent(string.Format("{0}/{1} experiments complete.", _filter.CompleteCount, _filter.DisplayExperiments.Count)));

			GUI.skin.horizontalScrollbarThumb.normal.background = _progressTexture;
			GUI.skin.horizontalScrollbarThumb.fixedHeight = 13;
			GUI.skin.horizontalScrollbar.fixedHeight = 13;
			ProgressBar ((float) _filter.CompleteCount / (float) _filter.DisplayExperiments.Count, GUILayout.ExpandWidth (true), GUILayout.Height(13));

			_filter.ShowLockedExperiments = GUILayout.Toggle(_filter.ShowLockedExperiments, new GUIContent("Show unresearched experiments"));
			_scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.scrollView);

			foreach (var experiment in _filter.DisplayExperiments) {
				DrawExperiment(experiment);
			}

			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUI.DragWindow();
		}

		private void DrawExperiment (Experiment exp) {
			GUI.skin.label.fontSize = 11;
			GUI.skin.label.fontStyle = FontStyle.Italic;
			GUI.skin.label.normal.textColor = exp.IsComplete ? Color.green : Color.yellow;
			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			GUILayout.Label(new GUIContent(exp.Description));
			GUILayout.FlexibleSpace();
			ProgressBar(exp.CompletedScience / exp.TotalScience, GUILayout.Width(100));
			GUILayout.EndHorizontal();
		}

		private void ProgressBar (float progress, params GUILayoutOption[] options) {
			GUI.skin.horizontalScrollbarThumb.normal.background = progress == 0 ? _emptyTexture : _progressTexture;
			GUILayout.HorizontalScrollbar(0, progress, 0, 1, options);
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
