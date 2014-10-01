using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ScienceChecklist {
	public sealed class ScienceWindow : MonoBehaviour {
		
		#region METHODS (PUBLIC)

		public void Awake () {
			_logger = new Logger(this);
			_logger.Trace("Awake");
		}

		public void Start () {
			_logger.Trace("Start");
			DontDestroyOnLoad(this);
		}

		public void OnApplicationQuit () {
			_logger.Trace("OnApplicationQuit");
		}

		public void Update () {
		}

		public void OnGUI () {
		}

		#endregion

		#region FIELDS

		private Logger _logger;

		#endregion
	}
}
