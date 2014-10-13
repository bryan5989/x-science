using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ScienceChecklist {
	internal static class TextureHelper {
		public static Texture2D FromResource (string resource, int width, int height) {
			var tex = new Texture2D(13, 13, TextureFormat.ARGB32, false);
			var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource).ReadToEnd();
			tex.LoadImage(iconStream);
			tex.Apply();
			return tex;
		}
	}
}
