using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ToolkitEngine.SceneManagement.VisualScripting
{
	/// <summary>
	/// Triggers the flow whenever the referenced <see cref="Transmission"/> is invoked.
	/// Subscribes when the graph starts listening and automatically unsubscribes when it stops.
	/// </summary>
	[UnitTitle("On Transmission")]
	[UnitCategory("Events/Scene Management")]
	public sealed class OnTransmission : EventUnit<EmptyEventArgs>
	{
		#region Ports

		/// <summary>The Transmission asset to listen to.</summary>
		[DoNotSerialize]
		[PortLabel("Transmission")]
		public ValueInput transmission { get; private set; }

		#endregion

		#region Properties

		// We manage subscriptions manually, so opt out of the global EventBus hook.
		protected override bool register => false;

		// GetHook is still required by the base class even when register = false.
		public override EventHook GetHook(GraphReference reference) => new EventHook(nameof(OnTransmission), reference.self);

		#endregion

		#region Methods

		protected override void Definition()
		{
			base.Definition();
			transmission = ValueInput<Transmission>(nameof(transmission), null);
		}

		public override void StartListening(GraphStack stack)
		{
			base.StartListening(stack);

			// Capture a persistent reference before the stack goes out of scope.
			var reference = stack.ToReference();
			var data = stack.GetElementData<Data>(this);

			var trans = Flow.FetchValue<Transmission>(transmission, reference);
			if (trans == null)
			{
				Debug.LogWarning($"[OnTransmission] No Transmission assigned — unit will not fire.", reference.self as UnityEngine.Object);
				return;
			}

			// Build the handler and keep both pieces so StopListening can clean up.
			data.Transmission = trans;
			data.Handler = () => Trigger(reference, new EmptyEventArgs());

			trans.Transmitted += data.Handler;
		}

		public override void StopListening(GraphStack stack)
		{
			var data = stack.GetElementData<Data>(this);

			if (data.Transmission != null && data.Handler != null)
			{
				data.Transmission.Transmitted -= data.Handler;
				data.Handler = null;
				data.Transmission = null;
			}

			base.StopListening(stack);
		}

		#endregion

		#region Structures

		/// <summary>
		/// Per-graph-instance state that survives between <see cref="StartListening"/>
		/// and <see cref="StopListening"/> calls.
		/// </summary>
		public new sealed class Data : EventUnit<EmptyEventArgs>.Data
		{
			public Transmission Transmission { get; set; }
			public Action Handler { get; set; }
		}

		public override IGraphElementData CreateData() => new Data();

		#endregion
	}
}