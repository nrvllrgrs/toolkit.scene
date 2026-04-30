using System;
using UnityEngine;

namespace ToolkitEngine.SceneManagement
{
    public class Transmission : ScriptableObject
    {
		#region Fields

		public Action Transmitted;

		#endregion

		#region Methods

		public void InvokeTransmitted() => Transmitted?.Invoke();

		#endregion
	}
}