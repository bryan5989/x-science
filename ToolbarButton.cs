using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;

namespace ScienceChecklist {
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public sealed class ToolbarButton : MonoBehaviour {
		
		#region METHODS (PUBLIC)
		
		public void Awake () {
			_logger = new Logger(this);
			_logger.Trace("Awake");
		}

		public void Start () {
			_logger.Trace("Start");
			DontDestroyOnLoad(this);

			GameEvents.onGUIApplicationLauncherReady.Add(AddApplication);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveApplication);
		}

		public void OnApplicationQuit () {
			_logger.Trace("OnApplicationQuit");
		}

		public void Update () {
		}

		public void OnGUI () {
		}

		#endregion

		#region METHODS (PRIVATE)

		private void AddApplication () {
			_logger.Trace("AddApplication");
			if (_button != null) {
				_logger.Debug("Mod application already added");
				return;
			}

			var texture = new Texture2D(38, 38, TextureFormat.ARGB32, false);
			texture.SetPixels32(Enumerable.Repeat((Color32) Color.green, 38 * 38).ToArray());
			texture.Apply();

			_logger.Info("Adding mod application");
			_button = ApplicationLauncher.Instance.AddModApplication(
				OnToggleOn,
				OnToggleOff,
				OnHover,
				OnDeHover,
				OnEnable,
				OnDisable,
				ApplicationLauncher.AppScenes.SPACECENTER,
				texture);
		}

		private void RemoveApplication () {
			if (_button == null) {
				_logger.Debug("Mod application already removed");
				return;
			}

			_logger.Info("Removing mod application");
			ApplicationLauncher.Instance.RemoveApplication(_button);
			_button = null;
		}

		private void OnToggleOn () {
			_logger.Trace("OnToggleOn");
		}

		private void OnToggleOff () {
			_logger.Trace("OnToggleOff");
		}

		private void OnHover () {
			_logger.Trace("OnHover");
		}

		private void OnDeHover () {
			_logger.Trace("OnDeHover");
		}

		private void OnEnable () {
			_logger.Trace("OnEnable");
		}

		private void OnDisable () {
			_logger.Trace("OnDisable");
		}

		#endregion

		#region FIELDS

		private ApplicationLauncherButton _button;
		private Logger _logger;

		#endregion
	}
}