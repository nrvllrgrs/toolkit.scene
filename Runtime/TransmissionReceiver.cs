using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace ToolkitEngine.SceneManagement
{
    public class TransmissionReceiver : MonoBehaviour
    {
		#region Fields

		[SerializeField, Required]
		private Transmission m_transmission;

		#endregion

		#region Events

		[SerializeField, Foldout("Events")]
		private UnityEvent m_onTransmitted;

		#endregion

		#region Methods

		private void OnEnable()
		{
			m_transmission.Transmitted += Transmitted;
		}

		private void OnDisable()
		{
			m_transmission.Transmitted -= Transmitted;
		}

		private void Transmitted()
		{
			m_onTransmitted?.Invoke();
		}

		#endregion
	}
}