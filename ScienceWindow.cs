using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	/// <summary>
	/// Renders a window containing experiments to the screen.
	/// </summary>
	internal sealed class ScienceWindow {
		/// <summary>
		/// Creates a new instance of the ScienceWindow class.
		/// </summary>
		public ScienceWindow () {
			_logger = new Logger(this);
			_rect = new Rect(40, 40, 500, 400);
			_scrollPos = new Vector2();
			_filter = new ExperimentFilter();
			_progressTexture = TextureHelper.FromResource("ScienceChecklist.scienceProgress.png", 13, 13);
			_completeTexture = TextureHelper.FromResource("ScienceChecklist.scienceComplete.png", 13, 13);
			_currentSituationTexture = TextureHelper.FromResource("ScienceChecklist.icons.currentSituation.png", 25, 21);
			_currentVesselTexture = TextureHelper.FromResource("ScienceChecklist.icons.currentVessel.png", 25, 21);
			_unlockedTexture = TextureHelper.FromResource("ScienceChecklist.icons.unlocked.png", 25, 21);
			_allTexture = TextureHelper.FromResource("ScienceChecklist.icons.all.png", 25, 21);
			_hideCompleteTexture = TextureHelper.FromResource("ScienceChecklist.icons.hideComplete.png", 25, 21);
			_showCompleteTexture = TextureHelper.FromResource("ScienceChecklist.icons.showComplete.png", 25, 21);
			_searchTexture = TextureHelper.FromResource("ScienceChecklist.icons.search.png", 25, 21);
			_clearSearchTexture = TextureHelper.FromResource("ScienceChecklist.icons.clearSearch.png", 25, 21);
			_settingsTexture = TextureHelper.FromResource("ScienceChecklist.icons.settings.png", 25, 21);
			_emptyTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			_emptyTexture.SetPixels(new[] { Color.clear });
			_emptyTexture.Apply();
			_settingsPanel = new SettingsPanel();
		}

		#region PROPERTIES

		/// <summary>
		/// Gets or sets a value indicating whether this window should be drawn.
		/// </summary>
		public bool IsVisible { get; set; }

		#endregion

		#region METHODS (PUBLIC)

		/// <summary>
		/// Draws the window if it is visible.
		/// </summary>
		public void Draw () {
			if (!IsVisible) {
				return;
			}

			if (_skin == null) {
				// Initialize our skin and styles.
				_skin = GameObject.Instantiate(HighLogic.Skin) as GUISkin;

				_skin.horizontalScrollbarThumb.fixedHeight = 13;
				_skin.horizontalScrollbar.fixedHeight = 13;

				_labelStyle = new GUIStyle(_skin.label) {
					fontSize = 11,
					fontStyle = FontStyle.Italic,
				};

				_progressLabelStyle = new GUIStyle(_skin.label) {
					fontStyle = FontStyle.BoldAndItalic,
					alignment = TextAnchor.MiddleCenter,
					fontSize = 11,
					normal = {
						textColor = new Color(0.322f, 0.298f, 0.004f),
					},
				};

				_situationStyle = new GUIStyle(_progressLabelStyle) {
					fontSize = 13,
					alignment = TextAnchor.MiddleLeft,
					fontStyle = FontStyle.Normal,
					fixedHeight = 25,
					contentOffset = new Vector2(0, 6),
					normal = {
						textColor = new Color(0.7f, 0.8f, 0.8f),
					},
				};

				_experimentProgressLabelStyle = new GUIStyle(_skin.label) {
					padding = new RectOffset(0, 0, 4, 0),
				};

				_horizontalScrollbarOnboardStyle = new GUIStyle(_skin.horizontalScrollbar) {
					normal = {
						background = _emptyTexture,
					},
				};
			}

			var oldSkin = GUI.skin;
			GUI.skin = _skin;

			_rect = GUILayout.Window(_windowId, _rect, DrawControls, "[x] Science!");

			if (!string.IsNullOrEmpty(_lastTooltip)) {
				_tooltipStyle = _tooltipStyle ?? new GUIStyle(_skin.window) {
					normal = {
						background = _emptyTexture,
					},
				};
				GUI.Window(_window2Id, new Rect(Mouse.screenPos.x + 15, Mouse.screenPos.y + 15, 500, 30), x => {
					GUI.Label(new Rect(), _lastTooltip);
				}, string.Empty, _tooltipStyle);
			}

			GUI.skin = oldSkin;
		}

		/// <summary>
		/// Refreshes the experiment cache.
		/// </summary>
		public void RefreshScience () {
			_logger.Trace("RefreshScience");
			_filter.RefreshExperiments();
		}

		/// <summary>
		/// Refreshes the experiment filter.
		/// </summary>
		public void RefreshFilter () {
			_logger.Trace("RefreshFilter");
			_filter.UpdateFilter();
		}

		/// <summary>
		/// Recalculates the current situation of the active vessel.
		/// </summary>
		public void RecalculateSituation () {
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
			var subBiome = string.IsNullOrEmpty(vessel.landedAt)
				? null
				: Vessel.GetLandedAtString(vessel.landedAt).Replace(" ", "");

			var dataCount = vessel.FindPartModulesImplementing<IScienceDataContainer>().Sum(x => x.GetData().Length);

			if (_lastDataCount != dataCount) {
				_lastDataCount = dataCount;
				RefreshScience();
			}

			if (_filter.CurrentSituation != null && _filter.CurrentSituation.Biome == biome && _filter.CurrentSituation.ExperimentSituation == situation && _filter.CurrentSituation.Body == body && _filter.CurrentSituation.SubBiome == subBiome) {
				return;
			}

			_filter.CurrentSituation = new Situation(body, situation, biome, subBiome);
		}

		#endregion

		#region METHODS (PRIVATE)

		/// <summary>
		/// Draws the controls inside the window.
		/// </summary>
		/// <param name="windowId"></param>
		private void DrawControls (int windowId) {
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical(GUILayout.Width(480), GUILayout.ExpandHeight(true));

			ProgressBar(
				new Rect (10, 27, 480, 13),
				_filter.TotalCount == 0 ? 1 : _filter.CompleteCount,
				_filter.TotalCount == 0 ? 1 : _filter.TotalCount,
				0,
				false);

			GUILayout.Space(20);

			GUILayout.BeginHorizontal();

			GUILayout.Label(string.Format("{0}/{1} complete.", _filter.CompleteCount, _filter.TotalCount), _experimentProgressLabelStyle, GUILayout.Width(150));
			GUILayout.FlexibleSpace();
			GUILayout.Label(new GUIContent(_searchTexture));
			_filter.Text = GUILayout.TextField(_filter.Text, GUILayout.Width(150));

			if (GUILayout.Button(new GUIContent(_clearSearchTexture, "Clear search"), GUILayout.Width(25), GUILayout.Height(23))) {
				_filter.Text = string.Empty;
			}

			GUILayout.EndHorizontal();

			_scrollPos = GUILayout.BeginScrollView(_scrollPos, _skin.scrollView);

			var i = 0;
			for (; i < _filter.DisplayExperiments.Count; i++) {
				var rect = new Rect(5, 20 * i, 500, 20);
				if (rect.yMax < _scrollPos.y || rect.yMin > _scrollPos.y + 400) {
					continue;
				}

				var experiment = _filter.DisplayExperiments[i];
				DrawExperiment(experiment, rect);
			}

			GUILayout.Space(20 * i);
			GUILayout.EndScrollView();
			
			GUILayout.BeginHorizontal();

			_filter.DisplayMode = (DisplayMode) GUILayout.SelectionGrid((int) _filter.DisplayMode, new[] {
				new GUIContent(_currentSituationTexture, "Show experiments available right now"),
				new GUIContent(_currentVesselTexture, "Show experiments available on this vessel"),
				new GUIContent(_unlockedTexture, "Show all unlocked experiments"),
				new GUIContent(_allTexture, "Show all experiments"),
			}, 4);

			GUILayout.FlexibleSpace();

			if (_filter.CurrentSituation != null) {
				var desc = _filter.CurrentSituation.Description;
				GUILayout.Label(char.ToUpper(desc[0]) + desc.Substring(1), _situationStyle);
			}

			GUILayout.FlexibleSpace();
			var toggleComplete = GUILayout.Button(new GUIContent(_filter.HideComplete ? _hideCompleteTexture : _showCompleteTexture, _filter.HideComplete ? "Currently hiding completed experiments" : "Currently showing completed experiments"), GUILayout.ExpandWidth(false));
			if (toggleComplete) {
				_filter.HideComplete = !_filter.HideComplete;
			}

			var toggleSettings = GUILayout.Button(new GUIContent(_settingsTexture, "Settings"));
			if (toggleSettings) {
				_showSettings = !_showSettings;
				_rect.width = _showSettings ? 700 : 500;
			}
			
			GUILayout.EndHorizontal();
			GUILayout.EndVertical ();

			if (_showSettings) {
				_settingsPanel.Draw();
			}

			GUILayout.EndHorizontal ();
			GUI.DragWindow();

			if (Event.current.type == EventType.Repaint && GUI.tooltip != _lastTooltip) {
				_lastTooltip = GUI.tooltip;
			}
			
			// If this window gets focus, it pushes the tooltip behind the window, which looks weird.
			// Just hide the tooltip while mouse buttons are held down to avoid this.
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) {
				_lastTooltip = string.Empty;
			}
		}

		/// <summary>
		/// Draws an experiment inside the given Rect.
		/// </summary>
		/// <param name="exp">The experiment to render.</param>
		/// <param name="rect">The rect inside which the experiment should be rendered.</param>
		private void DrawExperiment (Experiment exp, Rect rect) {
			_labelStyle.normal.textColor = exp.IsComplete ? Color.green : Color.yellow;
			var labelRect = new Rect(rect) {
				y = rect.y + 3,
			};
			var progressRect = new Rect(rect) {
				xMin = 395,
				xMax = 460,
				y = rect.y + 3,
			};

			GUI.Label(labelRect, exp.Description, _labelStyle);
			ProgressBar(progressRect, exp.CompletedScience, exp.TotalScience, exp.CompletedScience + exp.OnboardScience, true);
		}

		/// <summary>
		/// Draws a progress bar inside the given Rect.
		/// </summary>
		/// <param name="rect">The rect inside which the progress bar should be rendered.</param>
		/// <param name="curr">The completed progress value.</param>
		/// <param name="total">The total progress value.</param>
		/// <param name="curr2">The shaded progress value (used to show onboard science).</param>
		/// <param name="showValues">Whether to draw the curr and total values on top of the progress bar.</param>
		private void ProgressBar (Rect rect, float curr, float total, float curr2, bool showValues) {
			var complete = curr > total || (total - curr < 0.1);
			if (complete) {
				curr = total;
			}
			var progressRect = new Rect(rect) {
				y = rect.y + 1,
			};

			if (curr2 != 0 && !complete) {
				var complete2 = false;
				if (curr2 > total || (total - curr2 < 0.1)) {
					curr2 = total;
					complete2 = true;
				}
				_skin.horizontalScrollbarThumb.normal.background = curr2 < 0.1
					? _emptyTexture
					: complete2 ? _completeTexture : _progressTexture;

				GUI.HorizontalScrollbar(progressRect, 0, curr2 / total, 0, 1, _horizontalScrollbarOnboardStyle);
			}

			_skin.horizontalScrollbarThumb.normal.background = curr < 0.1
				? _emptyTexture
				: complete ? _completeTexture : _progressTexture;

			GUI.HorizontalScrollbar(progressRect, 0, curr / total, 0, 1);

			if (showValues) {
				var labelRect = new Rect(rect) {
					y = rect.y - 1,
				};
				GUI.Label(labelRect, string.Format("{0:0.#}  /  {1:0.#}", curr, total), _progressLabelStyle);
			}
		}

		#endregion

		#region FIELDS

		private Rect _rect;
		private Vector2 _scrollPos;
		private GUIStyle _labelStyle;
		private GUIStyle _horizontalScrollbarOnboardStyle;
		private GUIStyle _progressLabelStyle;
		private GUIStyle _situationStyle;
		private GUIStyle _experimentProgressLabelStyle;
		private GUIStyle _tooltipStyle;
		private GUISkin _skin;

		private string _lastTooltip;
		private bool _showSettings;
		private int _lastDataCount;

		private readonly Texture2D _progressTexture;
		private readonly Texture2D _completeTexture;
		private readonly Texture2D _emptyTexture;
		private readonly Texture2D _currentSituationTexture;
		private readonly Texture2D _currentVesselTexture;
		private readonly Texture2D _unlockedTexture;
		private readonly Texture2D _allTexture;
		private readonly Texture2D _hideCompleteTexture;
		private readonly Texture2D _showCompleteTexture;
		private readonly Texture2D _searchTexture;
		private readonly Texture2D _clearSearchTexture;
		private readonly Texture2D _settingsTexture;
		private readonly SettingsPanel _settingsPanel;

		private readonly ExperimentFilter _filter;
		private readonly Logger _logger;
		private readonly int _windowId = UnityEngine.Random.Range(0, int.MaxValue);
		private readonly int _window2Id = UnityEngine.Random.Range(0, int.MaxValue);

		#endregion
	}
}
