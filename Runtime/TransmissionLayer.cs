using System.Collections.Generic;
using UnityEngine;

namespace ToolkitEngine.SceneManagement
{
    public class TransmissionLayer : MonoBehaviour
    {
		#region Fields

		[SerializeField]
		private bool m_onByDefault = false;

		[SerializeField]
		private Transmission m_onTransmission;

		[SerializeField]
		private Transmission m_offTransmission;

		[SerializeField]
		private List<ObjectActivation> m_objects;

		#endregion

		#region 
		private void Start()
		{
			SetActive(m_onByDefault);
		}

		private void OnEnable()
		{
			m_onTransmission.Transmitted += TurnOn;
			m_offTransmission.Transmitted += TurnOff;
		}

		private void OnDisable()
		{
			m_onTransmission.Transmitted -= TurnOn;
			m_offTransmission.Transmitted -= TurnOff;
		}

		private void TurnOn() => SetActive(true);
		private void TurnOff() => SetActive(false);

		private void SetActive(bool value)
		{
			if (value)
			{
				foreach (var obj in m_objects)
				{
					obj.Set();
				}
			}
			else
			{
				foreach (var obj in m_objects)
				{
					obj.Invert();
				}
			}
		}

		#endregion
	}
}