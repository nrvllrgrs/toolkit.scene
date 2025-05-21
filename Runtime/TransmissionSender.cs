using NaughtyAttributes;
using UnityEngine;

namespace ToolkitEngine.SceneManagement
{
    public class TransmissionSender : MonoBehaviour
    {
		#region Fields

		[SerializeField, Required]
		private Transmission m_transmission;

		#endregion

		#region Methods

		[ContextMenu("Transmit")]
		public void Transmit()
		{
			m_transmission?.Transmitted?.Invoke();
		}

		#endregion
	}
}