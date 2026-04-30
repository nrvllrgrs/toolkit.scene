using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using NativeSceneManager = UnityEngine.SceneManagement.SceneManager;

namespace ToolkitEngine.SceneManagement
{
    public class SceneManager : Subsystem<SceneManager>
    {
		#region Fields

		private bool m_isLoading = false;
		private CancellationTokenSource m_cancellationTokenSource;

		#endregion

		#region Events

		public static event Action OnLoadingStarted;
		public static event Action OnLoadingCompleted;
		public static event Action<float> ProgressChanged;

		public static event Action<string> SceneLoading;
		public static event UnityAction<Scene, LoadSceneMode> SceneLoaded
		{
			add => NativeSceneManager.sceneLoaded += value;
			remove => NativeSceneManager.sceneLoaded -= value;
		}

		public static event Action<string> SceneReady;

		public static event Action<string> SceneUnloading;
		public static event UnityAction<Scene> SceneUnloaded
		{
			add => NativeSceneManager.sceneUnloaded += value;
			remove => NativeSceneManager.sceneUnloaded -= value;
		}

		public static event Action<Scene, Scene> ActiveSceneChanging;
		public static event UnityAction<Scene, Scene> ActiveSceneChanged
		{
			add => NativeSceneManager.activeSceneChanged += value;
			remove => NativeSceneManager.activeSceneChanged -= value;
		}

		#endregion

		#region Properties

		public static bool isLoading
		{
			get
			{
				if (!Exists)
					return false;

				return CastInstance.m_isLoading;
			}
			private set
			{
				// No change, skip
				if (isLoading == value)
					return;

				CastInstance.m_isLoading = value;
				(value ? OnLoadingStarted : OnLoadingCompleted)?.Invoke();
			}
		}

		public static Scene? previousActiveScene { get; private set; }
		public static float progress { get; private set; }

		#endregion

		#region Methods

		protected override void Initialize()
		{
			m_cancellationTokenSource = new CancellationTokenSource();
			NativeSceneManager.activeSceneChanged += HandleActiveSceneChanged;
		}

		private void HandleActiveSceneChanged(Scene from, Scene to)
		{
			previousActiveScene = from;
		}

		public static Scene GetActiveScene() => NativeSceneManager.GetActiveScene();

		public static bool SetActiveScene(Scene scene)
		{
			ActiveSceneChanging?.Invoke(GetActiveScene(), scene);
			return NativeSceneManager.SetActiveScene(scene);
		}

		public static Scene GetSceneByPath(string scenePath) => NativeSceneManager.GetSceneByPath(scenePath);

		public static Scene GetSceneByName(string name) => NativeSceneManager.GetSceneByName(name);

		public static Scene GetSceneByBuildIndex(int buildIndex) => NativeSceneManager.GetSceneByBuildIndex(buildIndex);

		public static Scene GetSceneAt(int index) => NativeSceneManager.GetSceneAt(index);

		public static int GetSceneCount() => NativeSceneManager.sceneCount;

		public static int GetSceneCountInBuildSettings() => NativeSceneManager.sceneCountInBuildSettings;

		/// <summary>
		/// Checks if a scene exists in build settings.
		/// </summary>
		public static bool SceneExistsInBuildSettings(string sceneName)
		{
			for (int i = 0; i < NativeSceneManager.sceneCountInBuildSettings; ++i)
			{
				string path = SceneUtility.GetScenePathByBuildIndex(i);
				string name = System.IO.Path.GetFileNameWithoutExtension(path);
				if (name == sceneName)
					return true;
			}
			return false;
		}

		#endregion

		#region Load Methods

		public static void LoadScene(SceneReference sceneRef)
		{
			_ = LoadSceneAsync(sceneRef);
		}

		public static void LoadScene(SceneReference sceneRef, LoadSceneMode mode)
		{
			_ = LoadSceneAsync(sceneRef, mode);
		}

		public static void LoadScene(string sceneName)
		{
			_ = LoadSceneAsync(sceneName, LoadSceneMode.Single);
		}

		public static void LoadScene(string sceneName, LoadSceneMode mode)
		{
			_ = LoadSceneAsync(sceneName, mode);
		}

		public static void LoadScene(int sceneBuildIndex)
		{
			_ = LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Single);
		}

		public static void LoadScene(int sceneBuildIndex, LoadSceneMode mode)
		{
			_ = LoadSceneAsync(sceneBuildIndex, mode);
		}

		public static async UniTask LoadSceneAsync(SceneReference sceneRef, CancellationToken ct = default)
		{
			await LoadSceneAsyncInternal(sceneRef.name, LoadSceneMode.Single, ct);
		}

		public static async UniTask LoadSceneAsync(SceneReference sceneRef, LoadSceneMode mode, CancellationToken ct = default)
		{
			await LoadSceneAsyncInternal(sceneRef.name, mode, ct);
		}

		public static async UniTask LoadSceneAsync(string sceneName, CancellationToken ct = default)
		{
			await LoadSceneAsyncInternal(sceneName, LoadSceneMode.Single, ct);
		}

		public static async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode, CancellationToken ct = default)
		{
			await LoadSceneAsyncInternal(sceneName, mode, ct);
		}

		public static async UniTask LoadSceneAsync(int sceneBuildIndex, CancellationToken ct = default)
		{
			await LoadSceneAsyncInternal(sceneBuildIndex, LoadSceneMode.Single, ct);
		}

		public static async UniTask LoadSceneAsync(int sceneBuildIndex, LoadSceneMode mode, CancellationToken ct = default)
		{
			await LoadSceneAsyncInternal(sceneBuildIndex, mode, ct);
		}

		private static async UniTask LoadSceneAsyncInternal(string sceneName, LoadSceneMode mode, CancellationToken ct)
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, CastInstance.m_cancellationTokenSource.Token);

			isLoading = true;
			progress = 0f;

			var loadOp = NativeSceneManager.LoadSceneAsync(sceneName, mode);
			SceneLoading?.Invoke(sceneName);

			await TrackLoadingProgress(loadOp, linkedCts.Token);

			await UniTask.NextFrame();
			SceneReady?.Invoke(sceneName);

			isLoading = false;
		}

		private static async UniTask LoadSceneAsyncInternal(int sceneBuildIndex, LoadSceneMode mode, CancellationToken ct)
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, CastInstance.m_cancellationTokenSource.Token);

			isLoading = true;
			progress = 0f;

			var loadOp = NativeSceneManager.LoadSceneAsync(sceneBuildIndex, mode);
			SceneLoading?.Invoke(GetSceneByBuildIndex(sceneBuildIndex).name);

			await TrackLoadingProgress(loadOp, linkedCts.Token);

			isLoading = false;
		}

		private static async UniTask TrackLoadingProgress(AsyncOperation operation, CancellationToken ct)
		{
			var progress = Progress.Create<float>(p =>
			{
				SceneManager.progress = p;
				ProgressChanged?.Invoke(p);
			});

			await operation.ToUniTask(progress, cancellationToken: ct);

			SceneManager.progress = 1f;
			ProgressChanged?.Invoke(SceneManager.progress);
		}

		/// <summary>
		/// Loads multiple scenes additively at once.
		/// </summary>
		public static async UniTask LoadScenesAsync(IEnumerable<string> sceneNames, CancellationToken ct = default)
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, CastInstance.m_cancellationTokenSource.Token);

			if (!sceneNames.Any())
				return;

			isLoading = true;
			progress = 0f;

			var operations = new List<(AsyncOperation op, string name)>();

			// Start all load operations
			foreach (string sceneName in sceneNames)
			{
				var op = NativeSceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
				operations.Add((op, sceneName));
				SceneLoading?.Invoke(sceneName);
			}

			// Track combined progress
			while (operations.Any(x => !x.op.isDone))
			{
				float totalProgress = 0f;
				foreach (var (op, name) in operations)
				{
					totalProgress += op.progress;
				}

				progress = totalProgress / operations.Count;
				ProgressChanged?.Invoke(progress);

				await UniTask.Yield(linkedCts.Token);
			}

			progress = 1f;
			ProgressChanged?.Invoke(progress);

			isLoading = false;
		}

		#endregion

		#region Unload Methods

		public static async UniTask UnloadSceneAsync(SceneReference sceneRef, CancellationToken ct = default)
		{
			await UnloadSceneAsync(sceneRef.name, ct);
		}

		public static async UniTask UnloadSceneAsync(string sceneName, CancellationToken ct = default)
		{
			await UnloadSceneAsync(NativeSceneManager.GetSceneByName(sceneName), ct);
		}

		public static async UniTask UnloadSceneAsync(int sceneBuildIndex, CancellationToken ct = default)
		{
			Scene scene = GetSceneByBuildIndex(sceneBuildIndex);
			SceneUnloading?.Invoke(scene.name);
			await NativeSceneManager.UnloadSceneAsync(sceneBuildIndex).ToUniTask(cancellationToken: ct);
		}

		public static async UniTask UnloadSceneAsync(Scene scene, CancellationToken ct = default)
		{
			SceneUnloading?.Invoke(scene.name);
			await NativeSceneManager.UnloadSceneAsync(scene).ToUniTask(cancellationToken: ct);
		}

		public static async UniTask UnloadSceneAsync(Scene scene, UnloadSceneOptions options, CancellationToken ct = default)
		{
			SceneUnloading?.Invoke(scene.name);
			await NativeSceneManager.UnloadSceneAsync(scene, options).ToUniTask(cancellationToken: ct);
		}

		#endregion

		#region Object Methods

		public static void MoveGameObjectToScene(GameObject go, Scene scene)
		{
			NativeSceneManager.MoveGameObjectToScene(go, scene);
		}

		#endregion
	}
}