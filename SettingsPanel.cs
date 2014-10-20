using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	internal sealed class SettingsPanel {
		public SettingsPanel () {
			_logger = new Logger(this);
		}

		public void Draw () {
			GUILayout.BeginVertical();

			GUILayout.Label("Nothing here yet...");

			GUILayout.EndVertical();
		}

		private readonly Logger _logger;
	}
}
