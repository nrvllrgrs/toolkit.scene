using Unity.VisualScripting;

namespace ToolkitEngine.SceneManagement.VisualScripting
{
	[UnitCategory("Scene Management")]
	public class Transmit : Unit
	{
		#region Ports

		[DoNotSerialize, PortLabelHidden]
		public ControlInput enter { get; set; }

		[DoNotSerialize, PortLabelHidden]
		public ControlOutput exit { get; set; }

		[DoNotSerialize, PortLabelHidden]
		public ValueInput transmission { get; set; }

		#endregion

		#region Methods

		protected override void Definition()
		{
			enter = ControlInput(nameof(enter), Trigger);
			exit = ControlOutput(nameof(exit));
			Succession(enter, exit);

			transmission = ValueInput<Transmission>(nameof(transmission), null);
			Requirement(transmission, enter);
		}

		private ControlOutput Trigger(Flow flow)
		{
			flow.GetValue<Transmission>(transmission)?.InvokeTransmitted();
			return exit;
		}

		#endregion
	}
}