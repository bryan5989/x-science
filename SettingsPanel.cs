using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist
{
	internal sealed class SettingsPanel {
		public SettingsPanel() {
			_logger = new Logger (this);
		}

		public void Draw () {
			GUILayout.BeginVertical();

			GUILayout.Label("Experimental features");
			ExperimentalFeatures.ShowInProgressScience = GUILayout.Toggle(ExperimentalFeatures.ShowInProgressScience, "Show in-progress science");

			GUILayout.EndVertical();
		}

		private readonly Logger _logger;
	}
}
