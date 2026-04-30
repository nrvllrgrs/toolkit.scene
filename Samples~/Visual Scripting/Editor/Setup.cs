using System;
using System.Collections.Generic;
using ToolkitEngine.SceneManagement;
using UnityEditor;

namespace ToolkitEditor.SceneManagement.VisualScripting
{
	[InitializeOnLoad]
	public static class Setup
	{
		static Setup()
		{
			var types = new List<Type>()
			{
				// Variables
				typeof(Transmission),
				typeof(TransmissionSender),
				typeof(TransmissionReceiver),
			};

			ToolkitEditor.VisualScripting.Setup.Initialize("ToolkitEngine.SceneManagement", types);
		}
	}
}