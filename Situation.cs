using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScienceChecklist {
	internal sealed class Situation {
		public Situation (CelestialBody body, ExperimentSituations situation, string biome = null) {
			_body = body;
			_situation = situation;
			_biome = biome;
			_formattedBiome = BiomeToString(_biome);
			_description = string.Format("{0} {1}{2}",
				ToString(_situation),
				Body.theName,
				string.IsNullOrEmpty(_formattedBiome) ? string.Empty : string.Format("'s {0}", _formattedBiome));
		}

		public CelestialBody        Body                { get { return _body; } }
		public ExperimentSituations ExperimentSituation { get { return _situation; } }
		public string               Biome               { get { return _biome; } }
		public string               FormattedBiome      { get { return _formattedBiome; } }
		public string               Description         { get { return _description; } }

		private string ToString (ExperimentSituations situation) {
			switch (situation) {
				case ExperimentSituations.FlyingHigh:
					return "flying high over";
				case ExperimentSituations.FlyingLow:
					return "flying low over";
				case ExperimentSituations.InSpaceHigh:
					return "in space high over";
				case ExperimentSituations.InSpaceLow:
					return "in space near";
				case ExperimentSituations.SrfLanded:
					return "landed at";
				case ExperimentSituations.SrfSplashed:
					return "splashed down at";
				default:
					return situation.ToString();
			}
		}

		private string BiomeToString (string biome) {
			return Regex.Replace(biome ?? string.Empty, "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1").Replace("  ", " ").Trim();
		}

		private readonly CelestialBody        _body;
		private readonly ExperimentSituations _situation;
		private readonly string               _biome;
		private readonly string               _formattedBiome;
		private readonly string               _description;
	}
}
