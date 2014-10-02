using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public sealed class ScienceChecklistAddon : MonoBehaviour {

		#region METHODS (PUBLIC)

		public void Awake () {
			_logger = new Logger(this);
			_logger.Trace("Awake");
		}

		public void Start () {
			_logger.Trace("Start");
			DontDestroyOnLoad(this);
			_button = new ToolbarButton();
			_button.Open += Button_Open;
			_button.Close += Button_Close;
		}

		public void OnApplicationQuit () {
			_logger.Trace("OnApplicationQuit");
			_button.Open -= Button_Open;
			_button.Close -= Button_Close;
		}

		public void Update () {
		}

		public void OnGUI () {
		}

		#endregion

		#region METHODS (PRIVATE)

		void Button_Open (object sender, EventArgs e) {
			_logger.Trace("Button_Open");
		}

		void Button_Close (object sender, EventArgs e) {
			_logger.Trace("Button_Close");
		}

		#endregion

		#region FIELDS

		private Logger _logger;
		private ToolbarButton _button;
		private ScienceWindow _window;

		#endregion
	}
}
