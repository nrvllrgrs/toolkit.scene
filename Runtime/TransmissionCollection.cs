using System.Collections.Generic;
using UnityEngine;

namespace ToolkitEngine.SceneManagement
{
	[CreateAssetMenu(menuName = "Toolkit/Scene/Transmission Collection")]
	public class TransmissionCollection : ScriptableObject
    {
		#region Fields

		[SerializeField]
		private List<Transmission> m_transmissions = new();

		#endregion

		#region Properties

		public IEnumerable<Transmission> transmissions => m_transmissions;

#if UNITY_EDITOR
		internal List<Transmission> transmissionList { get => m_transmissions; set => m_transmissions = value; }
#endif
		#endregion
	}
}