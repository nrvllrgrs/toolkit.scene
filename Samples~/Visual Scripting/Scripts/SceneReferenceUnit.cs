using Unity.VisualScripting;

namespace ToolkitEngine.SceneManagement.VisualScripting
{
	[UnitCategory("Scene Management")]
	[UnitTitle("Scene Reference")]
	public class SceneReferenceUnit : Unit
	{
		#region Fields

		[UnitHeaderInspectable]
		public SceneReference scene { get; set; }

		[DoNotSerialize]
		public ValueOutput name;

		[DoNotSerialize]
		public ValueOutput path;

		#endregion

		#region Methods

		protected override void Definition()
		{
			name = ValueOutput(nameof(name), (flow) => scene.name);
			path = ValueOutput(nameof(path), (flow) => scene.path);
		}

		#endregion
	}
}