using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	/// <summary>
	/// Panel for allowing users to edit settings.
	/// </summary>
	internal sealed class SettingsPanel {
		/// <summary>
		/// Instantiates a new instance of the SettingsPanel class.
		/// </summary>
		/// <param name="filter">The ExperimentFilter that this SettingsPanel will configure.</param>
		public SettingsPanel (ExperimentFilter filter) {
			_logger = new Logger(this);
			_filter = filter;
		}

		/// <summary>
		/// Renders this panel to the screen.
		/// </summary>
		public void Draw () {
			GUILayout.BeginVertical();

			_filter.HideComplete = GUILayout.Toggle(_filter.HideComplete, "Hide complete experiments");

			GUILayout.EndVertical();
		}

		private readonly ExperimentFilter _filter;
		private readonly Logger _logger;
	}
}
