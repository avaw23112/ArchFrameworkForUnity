using Arch.Tools;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations; // ������Ӹ������ռ�
namespace Arch
{
	/// <summary>
	/// ��Դ��������ڣ�����Addressables����+����+ʵ����+�ͷţ�
	/// </summary>
	public static partial class ArchRes
	{
		#region ����״̬������ȷ���̰߳�ȫ��״̬Ψһ��
		/// <summary>
		/// Addressables�Ƿ��ʼ�����
		/// </summary>
		private static bool _isAddressablesInited;

		/// <summary>
		/// ��Դӳ����Ƿ�������
		/// </summary>
		private static bool _isNameMapInited;

		/// <summary>
		/// ��Դӳ���ʵ��
		/// </summary>
		private static ResourceNameMap _nameMap;

		/// <summary>
		/// ���ڼ����еĲ�������������ظ����أ�
		/// </summary>
		private static readonly Dictionary<string, AsyncOperationHandle> _loadingHandles = new();

		/// <summary>
		/// ������أ���������Դ��Address����Сӳ��
		/// </summary>
		private static Dictionary<string, long> _updateSizeMap;

		/// <summary>
		/// ������أ��Ƿ��п��ø���
		/// </summary>
		private static bool _hasAvailableUpdate;

		/// <summary>
		/// ������أ��ܸ��´�С���ֽڣ�
		/// </summary>
		private static long _totalUpdateSize;

		/// <summary>
		/// ��ʼ������������߳��ظ���ʼ����
		/// </summary>
		private static readonly object _initLock = new();

		/// <summary>
		/// ��������������߳��ظ����/���ظ��£�
		/// </summary>
		private static readonly object _updateLock = new();
		#endregion

		#region ��ʼ��������Addressables+ӳ���ȷ��Ψһ��ʼ����
		/// <summary>
		/// ������ʼ����Դ��������Addressables��ʼ�� + ӳ�����أ�
		/// </summary>
		/// <param name="onError">��ʼ��ʧ�ܻص���������������Ϣ��</param>
		public static async UniTask<bool> InitializeAsync(Action<string> onError = null)
		{
			// �����������ʼ����ֱ�ӷ���
			if (_isAddressablesInited && _isNameMapInited)
				return true;

			try
			{
				// 1. �ȳ�ʼ��Addressables�������һ�����������������Ч��
				if (!await InitAddressablesAsync(onError))
					return false;
				// 2. �ټ�����Դӳ�������Addressables��
				if (!LoadResourceNameMapAsync(onError))
					return false;
				_isNameMapInited = true;
				ArchLog.Debug($"ArchRes ������ʼ����ɣ�");
				return true;
			}
			catch (Exception ex)
			{
				onError?.Invoke($"��ʼ���쳣��{ex.Message}\n{ex.StackTrace}");
				ArchLog.Error($"ArchRes InitializeAsync Exception: {ex}");
				return false;
			}
		}

		/// <summary>
		/// ������ʼ��Addressables���ڲ����ã��ⲿ������InitializeAsync��
		/// </summary>
		private static async UniTask<bool> InitAddressablesAsync(Action<string> onError)
		{

			if (_isAddressablesInited)
				return true;

			// Addressables��ʼ������Ӧԭ�ű���InitializeAsync��
			var initHandle = Addressables.InitializeAsync();
			await initHandle;

			_isAddressablesInited = initHandle.Status == AsyncOperationStatus.Succeeded;

			if (!_isAddressablesInited)
			{
				string errorMsg = $"Addressables��ʼ��ʧ�ܣ�{initHandle.OperationException?.Message ?? "δ֪����"}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				// �ͷ�ʧ�ܵľ��
				Addressables.Release(initHandle);
			}

			return _isAddressablesInited;
		}

		/// <summary>
		/// ������Դӳ����ڲ����ã�����Addressables��ʼ����
		/// </summary>
		private static bool LoadResourceNameMapAsync(Action<string> onError)
		{
			if (_isNameMapInited || _nameMap != null)
				return true;

			// ��Addressables����ӳ���ԭ�߼��Ǵ�Resources������ͳһ��Addressables���淶��
			_nameMap = Resources.Load<ResourceNameMap>("ResourceNameMap");

			if (_nameMap == null)
			{
				string errorMsg = $"��Դӳ������ʧ�ܣ� ӳ������ڣ�����ִ�С�Tools��������Դ����ӳ���";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				Resources.UnloadAsset(_nameMap);
				return false;
			}

			ArchLog.Debug($"��Դӳ��������ɣ����� {_nameMap.GetAllMappings().Count} ����Դӳ��");
			return true;
		}
		#endregion
		/// <summary>
		/// ��Ⲣ������Դ���£�����1.21.21������InvalidKeyException��
		/// </summary>
		public static async UniTask<bool> CheckForUpdatesAsync(string preloadLabel = "preload", Action<string> onError = null)
		{
			System.Threading.CancellationToken cancellationToken = new System.Threading.CancellationToken();

			try
			{
				// 1. ���Catalog�Ƿ��и��£�������Ҫ���µ�Catalog�б�
				var catalogCheckHandle = Addressables.CheckForCatalogUpdates(false);
				await catalogCheckHandle;

				if (!catalogCheckHandle.IsValid() || catalogCheckHandle.Status != AsyncOperationStatus.Succeeded)
				{
					string error = $"Catalog���ʧ�ܣ�{catalogCheckHandle.OperationException?.Message}";
					onError?.Invoke(error);
					ArchLog.Error(error);
					Addressables.Release(catalogCheckHandle);
					return false;
				}

				// 2. ����Catalog���£���ִ�и��£����������Դ��λ���ܹ�ʱ��
				if (catalogCheckHandle.Result.Count > 0)
				{
					var catalogUpdateHandle = Addressables.UpdateCatalogs(catalogCheckHandle.Result, false);
					await catalogUpdateHandle;

					if (!catalogUpdateHandle.IsValid() || catalogUpdateHandle.Status != AsyncOperationStatus.Succeeded)
					{
						string error = $"Catalog����ʧ�ܣ�{catalogUpdateHandle.OperationException?.Message}";
						onError?.Invoke(error);
						ArchLog.Error(error);
						Addressables.Release(catalogUpdateHandle);
						Addressables.Release(catalogCheckHandle);
						return false;
					}

					ArchLog.Debug($"�Ѹ���{catalogUpdateHandle.Result.Count}��Catalog");
					Addressables.Release(catalogUpdateHandle);
				}

				// 3. ��ȡԤ������Դ����ЧKey���ؼ�������ԴKey����Catalog�б�
				var preloadKeys = await GetValidPreloadKeysAsync(preloadLabel);
				if (preloadKeys.Count == 0)
				{
					ArchLog.Debug($"Ԥ���ر�ǩ[{preloadLabel}]������Ч��Դ���������");
					Addressables.Release(catalogCheckHandle);
					return true; // ����Դ����£���Ϊ�ɹ�
				}

				// 4. ������Դ������������Ч����ԴKey������Catalog�б�
				var downloadHandle = Addressables.DownloadDependenciesAsync(
					keys: preloadKeys,
					mode: Addressables.MergeMode.Union
				).WithCancellation(cancellationToken);

				await downloadHandle;

				if (downloadHandle.Status.IsFaulted())
				{
					string error = $"��Դ��������ʧ�ܣ�{downloadHandle.Status}";
					onError?.Invoke(error);
					ArchLog.Error(error);
					Addressables.Release(downloadHandle);
					Addressables.Release(catalogCheckHandle);
					return false;
				}

				ArchLog.Debug($"�ɹ�����ɣ���{preloadKeys.Count}����Դ");
				Addressables.Release(downloadHandle);
				Addressables.Release(catalogCheckHandle);
				return true;
			}
			catch (InvalidKeyException ex)
			{
				// ר���Ŵ�����ЧKey�쳣��ͨ������Դ���ô���
				string error = $"��Ч����ԴKey��{ex.Message}��������Դ����";
				onError?.Invoke(error);
				ArchLog.Error(error);
				return false;
			}
			catch (Exception ex)
			{
				string error = $"���¼���쳣��{ex.Message}\n{ex.StackTrace}";
				onError?.Invoke(error);
				ArchLog.Error(error);
				return false;
			}
		}

		/// <summary>
		/// ��ȡԤ���ر�ǩ�µ���Ч��ԴKey��ȷ������ЧKey���µ��쳣��
		/// </summary>
		private static async UniTask<List<string>> GetValidPreloadKeysAsync(string preloadLabel)
		{
			var validKeys = new List<string>();

			// ��ȡ��ǩ��Ӧ����Դ��λ
			var locsHandle = Addressables.LoadResourceLocationsAsync(preloadLabel);
			await locsHandle;

			if (locsHandle.IsValid() && locsHandle.Status == AsyncOperationStatus.Succeeded)
			{
				// ������Ч��λ����ȡΨһKey
				foreach (var loc in locsHandle.Result)
				{
					if (loc != null && !string.IsNullOrEmpty(loc.PrimaryKey) && !validKeys.Contains(loc.PrimaryKey))
					{
						validKeys.Add(loc.PrimaryKey);
					}
				}
			}

			Addressables.Release(locsHandle);
			return validKeys;
		}



		/// <summary>
		/// �ݹ���֤��Դ��λ���������������Ƿ���Ч����null��
		/// </summary>
		private static bool IsValidLocation(IResourceLocation loc)
		{
			// ������֤����λ����Ϊnull��PrimaryKey��Ч
			if (loc == null || string.IsNullOrEmpty(loc.PrimaryKey) || loc.ResourceType == null)
				return false;

			// �ݹ���֤����������
			if (loc.Dependencies != null)
			{
				foreach (var dep in loc.Dependencies)
				{
					// ���κ�һ��������Ч����ǰ��λ��Ч
					if (!IsValidLocation(dep))
						return false;
				}
			}

			return true;
		}





		/// <summary>
		/// �������м�⵽�ĸ�����Դ
		/// </summary>
		/// <param name="progressCallback">���ؽ��Ȼص���0~1��</param>
		/// <param name="onError">����ʧ�ܻص�</param>
		/// <returns>�����Ƿ�ɹ�</returns>
		public static async UniTask<bool> DownloadUpdatesAsync(Action<float> progressCallback = null, Action<string> onError = null)
		{
			lock (_updateLock)
			{
				// �޿��ø��£�ֱ�ӷ��سɹ�
				if (!_hasAvailableUpdate || _updateSizeMap.Count == 0)
				{
					progressCallback?.Invoke(1f); // ������Ϊ100%
					ArchLog.Debug("�޿��ø��£���������");
					return true;
				}
			}

			// ȷ��Addressables�ѳ�ʼ��
			if (!await InitAddressablesAsync(onError))
				return false;

			AsyncOperationHandle downloadHandle = default;
			try
			{
				// 1. ׼�������б��Ӹ��´�Сӳ���л�ȡ������Address��
				var downloadKeys = new List<string>();
				lock (_updateLock)
				{
					downloadKeys.AddRange(_updateSizeMap.Keys);
				}

				ArchLog.Debug($"��ʼ���ظ��£�{downloadKeys.Count} ����Դ���ܴ�С {FormatFileSize(_totalUpdateSize)}");

				// 2. ִ�����أ��ϲ�ģʽ��Union�����Զ��ͷž����
				downloadHandle = Addressables.DownloadDependenciesAsync(
					keys: downloadKeys,
					mode: Addressables.MergeMode.Union,
					autoReleaseHandle: false
				);

				// 3. ʵʱ�ص����ؽ��ȣ���Ӧԭ�ű���whileѭ����
				while (!downloadHandle.IsDone)
				{
					// ������Ч���ȣ���������0%�����ڳ���100%�������
					float safeProgress = Mathf.Clamp01(downloadHandle.PercentComplete);
					progressCallback?.Invoke(safeProgress);
					await UniTask.Yield(); // ���yield return null��UniTask����Ч
				}

				// 4. ������ɺ������Ϊ100%
				progressCallback?.Invoke(1f);

				// 5. ������ؽ��
				if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
				{
					lock (_updateLock)
					{
						_hasAvailableUpdate = false; // ���ø���״̬
						_updateSizeMap.Clear();
					}

					ArchLog.Debug("���и�����Դ������ɣ�");
					return true;
				}
				else
				{
					string errorMsg = $"����ʧ�ܣ�{downloadHandle.OperationException?.Message ?? "δ֪����"}";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					return false;
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"�����쳣��{ex.Message}\n{ex.StackTrace}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return false;
			}
			finally
			{
				// ���۳ɹ�ʧ�ܣ����ͷ����ؾ���������ڴ�й©��
				if (downloadHandle.IsValid())
					Addressables.Release(downloadHandle);
			}
		}

		/// <summary>
		/// ��ȡ�ܸ��´�С������λ��ʽ������"1.2MB"��
		/// </summary>
		public static string GetFormattedTotalUpdateSize()
		{
			lock (_updateLock)
			{
				return FormatFileSize(_totalUpdateSize);
			}
		}

		/// <summary>
		/// ��ȡԭʼ�ܸ��´�С���ֽڣ�
		/// </summary>
		public static long GetRawTotalUpdateSize()
		{
			lock (_updateLock)
			{
				return _totalUpdateSize;
			}
		}

		/// <summary>
		/// ���߷�������ʽ���ļ���С���ֽڡ�KB/MB/GB��
		/// </summary>
		private static string FormatFileSize(long bytes)
		{
			if (bytes < 1024)
				return $"{bytes} B";
			else if (bytes < 1024 * 1024)
				return $"{(bytes / 1024f):F2} KB";
			else if (bytes < 1024 * 1024 * 1024)
				return $"{(bytes / (1024f * 1024f)):F2} MB";
			else
				return $"{(bytes / (1024f * 1024f * 1024f)):F2} GB";
		}

		/// <summary>
		/// ����Addressables Label���ظñ�ǩ�µ�������Դ���Զ�������Ч��Դ��
		/// </summary>
		/// <typeparam name="T">��Դ���ͣ���Texture2D��GameObject��AudioClip�ȣ�</typeparam>
		/// <param name="label">Ŀ��Label��Addressables�����õı�ǩ��</param>
		/// <param name="progressCallback">���ؽ��Ȼص�������1���Ѽ�������������2����������</param>
		/// <param name="onLoaded">ȫ��������ɻص������سɹ����ص���Դ�б�</param>
		/// <param name="onError">����ʧ�ܻص���������������Ϣ��</param>
		/// <returns>�ɹ����ص���Դ�б����б��ʾ����Դ��ȫ������ʧ�ܣ�</returns>
		public static async UniTask<List<T>> LoadAllByLabelAsync<T>(
			string label,
			Action<int, int> progressCallback = null,
			Action<List<T>> onLoaded = null,
			Action<string> onError = null) where T : UnityEngine.Object
		{
			// 1. ȷ����������������ʼ����Addressables+ӳ���
			if (!await InitializeAsync(onError))
				return new List<T>();

			// 2. У��Label��Ч��
			if (string.IsNullOrEmpty(label))
			{
				string errorMsg = "������Դʧ�ܣ�Label����Ϊ��";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return new List<T>();
			}

			// �ؼ�����1��ʹ��IList<IResourceLocation>���գ����Ƴ�throwExceptionIfNothingFound����
			IList<IResourceLocation> resourceLocs = null;
			try
			{
				// 3. ��ȡ��Label�µ�������Դ��λ������API�����ͷ������ͣ�
				var locsHandle = Addressables.LoadResourceLocationsAsync(
					key: label,
					type: typeof(T) // ����ָ�����͵���Դ
				);

				await locsHandle;

				if (locsHandle.Status != AsyncOperationStatus.Succeeded)
				{
					string errorMsg = $"��ȡLabel[{label}]����Դ��λʧ�ܣ�{locsHandle.OperationException?.Message}";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					Addressables.Release(locsHandle);
					return new List<T>();
				}

				resourceLocs = locsHandle.Result;
				Addressables.Release(locsHandle);

				// 4. ��������Դ����
				int totalCount = resourceLocs?.Count ?? 0;
				if (totalCount == 0)
				{
					string tipMsg = $"Label[{label}]����{typeof(T).Name}���͵���Դ";
					ArchLog.Debug(tipMsg);
					onLoaded?.Invoke(new List<T>());
					return new List<T>();
				}

				// 5. ��ʼ�����������б�����ȸ���
				var loadTasks = new List<UniTask<T>>();
				var loadedResources = new List<T>();
				int completedCount = 0;

				// 6. Ϊÿ����Դ��λ����������������IResourceLocation����ʹ�ã�
				foreach (var loc in resourceLocs)
				{
					if (loc == null || string.IsNullOrEmpty(loc.PrimaryKey)) // �ؼ�����2��ʹ��IResourceLocation��PrimaryKey
					{
						ArchLog.Warning($"Label[{label}]������Ч��Դ��λ��������");
						completedCount++;
						progressCallback?.Invoke(completedCount, totalCount);
						continue;
					}

					// 6.1 ����Ƿ����и���Դ�ļ�������
					bool hasExistingTask = false;
					lock (_loadingHandles)
					{
						if (_loadingHandles.TryGetValue(loc.PrimaryKey, out var existingHandle))
						{
							if (!existingHandle.IsDone)
							{
								loadTasks.Add(WaitForExistingLoad<T>(existingHandle, null, null));
								hasExistingTask = true;
							}
							else
							{
								_loadingHandles.Remove(loc.PrimaryKey);
							}
						}
					}

					// 6.2 ���������񣬴����¼�������
					if (!hasExistingTask)
					{
						loadTasks.Add(LoadSingleResourceByLocAsync<T>(loc, (resource) =>
						{
							lock (_loadingHandles)
							{
								_loadingHandles.Remove(loc.PrimaryKey);
							}

							completedCount++;
							if (resource != null)
								loadedResources.Add(resource);

							progressCallback?.Invoke(completedCount, totalCount);
						}, errorMsg =>
						{
							lock (_loadingHandles)
							{
								_loadingHandles.Remove(loc.PrimaryKey);
							}

							completedCount++;
							progressCallback?.Invoke(completedCount, totalCount);
							ArchLog.Warning($"Label[{label}]����Դ[{loc.PrimaryKey}]����ʧ�ܣ�{errorMsg}");
						}));
					}
				}

				// 7. �ȴ����м����������
				await UniTask.WhenAll(loadTasks);

				// 8. ���ջص�����־
				string resultMsg = $"Label[{label}]��Դ������ɣ���{totalCount}�����ɹ�{loadedResources.Count}����ʧ��{totalCount - loadedResources.Count}��";
				ArchLog.Debug(resultMsg);
				onLoaded?.Invoke(loadedResources);
				return loadedResources;
			}
			catch (Exception ex)
			{
				string errorMsg = $"Label[{label}]��Դ�����쳣��{ex.Message}\n{ex.StackTrace}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return new List<T>();
			}
			finally
			{
				// ������Դ��λ����
				if (resourceLocs != null && resourceLocs is List<IResourceLocation> listLocs)
					listLocs.Clear();
			}
		}

		/// <summary>
		/// ����������ͨ��IResourceLocation���ص�����Դ���������ͣ�
		/// </summary>
		private static async UniTask<T> LoadSingleResourceByLocAsync<T>(
			IResourceLocation loc, // �ؼ�����3���������͸�ΪIResourceLocation
			Action<T> onSingleLoaded,
			Action<string> onSingleError) where T : UnityEngine.Object
		{
			if (loc == null || string.IsNullOrEmpty(loc.PrimaryKey))
			{
				onSingleError?.Invoke("��Ч����Դ��λ");
				return null;
			}

			// �������ؾ������¼���ֵ�
			var loadHandle = Addressables.LoadAssetAsync<T>(loc.PrimaryKey);
			lock (_loadingHandles)
			{
				_loadingHandles[loc.PrimaryKey] = loadHandle;
			}

			try
			{
				await loadHandle;

				if (loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null)
				{
					T resource = loadHandle.Result;
					onSingleLoaded?.Invoke(resource);
					ArchLog.Debug($"Label��Դ���سɹ���{loc.PrimaryKey}�����ͣ�{typeof(T).Name}��");
					return resource;
				}
				else
				{
					string errorMsg = loadHandle.OperationException?.Message ?? "��Դ���سɹ������Ϊ��";
					onSingleError?.Invoke(errorMsg);
					return null;
				}
			}
			catch (Exception ex)
			{
				onSingleError?.Invoke(ex.Message);
				return null;
			}
			finally
			{
				// ������ʧ�ܣ��ͷž��
				if (loadHandle.IsValid() && (loadHandle.Status == AsyncOperationStatus.Failed || loadHandle.Result == null))
					Addressables.Release(loadHandle);
			}
		}
		#region ��Դ����/ʵ����/�ͷţ�����ԭ���߼����Ż�������

		/// <summary>
		/// ͨ����Դ�����첽������Դ���������ü������Զ������Ѽ�����Դ��
		/// </summary>
		public static async UniTask<T> LoadAsync<T>(string resourceName, Action<T> onLoaded = null, Action<string> onError = null) where T : UnityEngine.Object
		{
			// 1. ȷ����������������ʼ��
			if (!await InitializeAsync(onError))
				return null;

			// 2. ��ӳ����ȡAddress
			if (!_nameMap.TryGetAddress(resourceName, out string address))
			{
				string errorMsg = $"��Դ {resourceName} ��������ӳ�����";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return null;
			}

			// 3. �����ظ����أ�����������Դ�����ü���+1

			if (_resourceRecords.TryGetValue(address, out var existingRecord))
			{
				// ���ü���+1
				existingRecord.RefCount++;
				ArchLog.Debug($"������Դ {resourceName}��Address��{address}������ǰ���ü�����{existingRecord.RefCount}");

				// �ȴ�����Handle��ɣ������ڼ����У�
				if (!existingRecord.Handle.IsDone)
				{

					// �첽�ȴ�Handle��ɣ����������̣߳�
					var waitTask = UniTask.Create(async () =>
					{
						while (!existingRecord.Handle.IsDone)
							await UniTask.Yield();
					});
					waitTask.Forget(); // �������ȴ�
				}

				// ��ȡ������ص�
				var result = existingRecord.Handle.Result as T;
				onLoaded?.Invoke(result);
				return result;
			}


			// 4. �״μ��أ������µ���Դ��¼
			AsyncOperationHandle<T> loadHandle = default;
			try
			{
				loadHandle = Addressables.LoadAssetAsync<T>(address);

				// �ȴ��������
				var result = await loadHandle;

				if (result != null)
				{
					lock (_recordLock)
					{
						// ˫�ؼ�飺������̲߳��������ظ���¼
						if (!_resourceRecords.ContainsKey(address))
						{
							// ������Դ��¼�������ֵ�
							var newRecord = new ResourceRecord(address, loadHandle);
							_resourceRecords.Add(address, newRecord);
							ArchLog.Debug($"�״μ�����Դ {resourceName}��Address��{address}�������ü�����1");
						}
						else
						{
							// ���̲߳����������������м�¼�����ü���+1
							_resourceRecords[address].RefCount++;
							ArchLog.Debug($"���̲߳���������Դ {resourceName}����ǰ���ü�����{_resourceRecords[address].RefCount}");
						}
					}

					onLoaded?.Invoke(result);
					return result;
				}
				else
				{
					string errorMsg = $"��Դ {resourceName}��Address��{address}�����سɹ������Ϊ��";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					return null;
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"��Դ {resourceName}��Address��{address}�������쳣��{ex.Message}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);

				// ����ʧ�ܣ��ͷ�Handle�������ڴ�й©
				if (loadHandle.IsValid())
					Addressables.Release(loadHandle);

				return null;
			}
		}


		/// <summary>
		/// �ȴ����м��ز�����ɣ������ظ����أ�
		/// </summary>
		private static async UniTask<T> WaitForExistingLoad<T>(AsyncOperationHandle existingHandle, Action<T> onLoaded, Action<string> onError) where T : UnityEngine.Object
		{
			try
			{
				await existingHandle;
				var result = existingHandle.Result as T;

				if (result != null)
				{
					onLoaded?.Invoke(result);
					ArchLog.Debug($"��Դ������ɣ��������в�������{result.name}");
					return result;
				}
				else
				{
					string errorMsg = $"�������м��ز���ʧ�ܣ����Ϊ��";
					onError?.Invoke(errorMsg);
					ArchLog.Error(errorMsg);
					return null;
				}
			}
			catch (Exception ex)
			{
				string errorMsg = $"�������м��ز����쳣��{ex.Message}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return null;
			}
		}

		/// <summary>
		/// ͨ����Դ����ʵ����Ԥ���壨����ʵ�����������ü�����
		/// </summary>
		public static async UniTask<GameObject> InstantiateAsync(
			string resourceName,
			Transform parent = null,
			bool worldPositionStays = true,
			Action<GameObject> onInstantiated = null,
			Action<string> onError = null)
		{
			// 1. �ȼ���Ԥ���壨�Զ��������ü�����
			var prefab = await LoadAsync<GameObject>(resourceName, onError: onError);
			if (prefab == null)
				return null;

			try
			{
				// 2. ʵ����Ԥ����
				var instance = GameObject.Instantiate(prefab, parent, worldPositionStays);
				instance.name = prefab.name; // �Ƴ�"(Clone)"��׺

				// 3. ��ʵ����ӵ���Դ��¼��ʵ���б�
				lock (_recordLock)
				{
					if (_nameMap.TryGetAddress(resourceName, out string address) && _resourceRecords.TryGetValue(address, out var record))
					{
						record.AddInstance(instance);
					}
					else
					{
						ArchLog.Warning($"ʵ������Դ {resourceName} δ�ҵ���Ӧ��¼���޷�����ʵ���������ѱ��ͷţ�");
					}
				}

				onInstantiated?.Invoke(instance);
				return instance;
			}
			catch (Exception ex)
			{
				string errorMsg = $"Ԥ���� {resourceName} ʵ�����쳣��{ex.Message}";
				onError?.Invoke(errorMsg);
				ArchLog.Error(errorMsg);
				return null;
			}
		}


		/// <summary>
		/// �ͷ���Դ���������ü���������Ϊ0ʱ�����ͷŵײ���Դ��
		/// </summary>
		public static void Release<T>(string resourceName, T instance = null, bool releaseAsset = false) where T : UnityEngine.Object
		{
			// 1. ����ʵ���ͷţ�������ʵ����
			if (instance != null)
			{
				bool isInstanceRemoved = false;
				string targetAddress = null;

				lock (_recordLock)
				{
					// �ҵ�ʵ����Ӧ����Դ��¼
					if (_nameMap.TryGetAddress(resourceName, out targetAddress) && _resourceRecords.TryGetValue(targetAddress, out var record))
					{
						// �Ӽ�¼���Ƴ�ʵ��
						isInstanceRemoved = record.RemoveInstance(instance);
						if (isInstanceRemoved)
						{
							ArchLog.Debug($"����Դ {resourceName}��Address��{targetAddress}���Ƴ�ʵ����ʣ��ʵ������{record.InstanceObjects.Count}");
						}
					}
				}

				// ����ʵ���������Ƿ��ҵ���¼�������ڴ�й©��
				if (isInstanceRemoved || !_isNameMapInited)
				{
					if (instance is GameObject go)
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							GameObject.DestroyImmediate(go);
						else
#endif
							GameObject.Destroy(go);
					}
					else
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							UnityEngine.Object.DestroyImmediate(instance);
						else
#endif
							UnityEngine.Object.Destroy(instance);
					}
				}
				else
				{
					ArchLog.Warning($"�ͷ�ʵ�� {instance.name} ʧ�ܣ�δ�ҵ���Ӧ��Դ��¼�������ѱ��ͷ�");
					return;
				}
			}

			// 2. ����ײ���Դ�ͷţ����ü���Ϊ0��ǿ���ͷ�ʱ��
			if (!_nameMap.TryGetAddress(resourceName, out string address))
			{
				ArchLog.Warning($"�ͷŵײ���Դʧ�ܣ�{resourceName} ��������ӳ�����");
				return;
			}

			lock (_recordLock)
			{
				if (!_resourceRecords.TryGetValue(address, out var record))
				{
					ArchLog.Warning($"�ͷŵײ���Դʧ�ܣ�{resourceName}��Address��{address}������Դ��¼���������ͷ�");
					return;
				}

				// ���ü���-1
				record.RefCount--;

				// ���������ü�����0 �� ǿ���ͷţ�releaseAsset=true��
				if (record.RefCount <= 0 || releaseAsset)
				{
					// ����1����������ʣ��ʵ��
					record.DestroyAllInstances();

					// ����2���ͷŵײ���ԴHandle
					record.ReleaseHandle();

					// ����3�����ֵ����Ƴ���¼
					_resourceRecords.Remove(address);
					ArchLog.Debug($"��Դ {resourceName}��Address��{address}���ѳ����ͷţ����ü�����0��ǿ���ͷţ�");
				}
			}
		}
		#region ������ǿ���ͷ�������Դ
		/// <summary>
		/// ǿ���ͷ������Ѽ�����Դ��
		/// 1. ��������ʵ����Object�������Ƿ���ʹ�ã�
		/// 2. �ͷ�������ԴHandle
		/// 3. �����Դ��¼�ֵ�
		/// </summary>
		public static void ForceRelease()
		{
			lock (_recordLock)
			{
				if (_resourceRecords.Count == 0)
				{
					ArchLog.Debug("ForceRelease�����Ѽ�����Դ�������ͷ�");
					return;
				}

				// ����������Դ��¼��ǿ�����ٲ��ͷ�
				foreach (var (address, record) in _resourceRecords)
				{
					try
					{
						// 1. ǿ����������ʵ��
						record.DestroyAllInstances();

						// 2. ǿ���ͷ�Handle
						record.ReleaseHandle();

						ArchLog.Debug($"ForceRelease����Դ {address} ǿ���ͷ����");
					}
					catch (Exception ex)
					{
						ArchLog.Error($"ForceRelease��������Դ {address} �쳣��{ex.Message}\n{ex.StackTrace}");
					}
				}

				// 3. ���������Դ��¼
				_resourceRecords.Clear();
				ArchLog.Debug($"ForceRelease��������Դ��¼����գ����ͷ� {_resourceRecords.Count} ����Դ");
			}

			// �����������ó�ʼ��״̬����ѡ������ҵ����������Ƿ�����
			// _isAddressablesInited = false;
			// _isNameMapInited = false;
			// _nameMap = null;

			ArchLog.Debug("ForceRelease��ǿ���ͷ��������");
		}
		#endregion


		#endregion
		#region ��������Դ���ü�����ʵ�����ٺ��Ľṹ
		/// <summary>
		/// ��Դ��¼����������Դ�����ü�����Handle��ʵ��������
		/// </summary>
		private class ResourceRecord
		{
			/// <summary>
			/// ���ü���������һ��+1���ͷ�һ��-1��Ϊ0ʱ��������Դ��
			/// </summary>
			public int RefCount { get; set; }

			/// <summary>
			/// ��Դ��Addressables Handle
			/// </summary>
			public AsyncOperationHandle Handle { get; private set; }

			/// <summary>
			/// ����Դʵ������������Object����Ԥ����ʵ����
			/// </summary>
			public List<UnityEngine.Object> InstanceObjects { get; private set; }

			/// <summary>
			/// ��Դ��Ӧ��Address������������
			/// </summary>
			public string Address { get; private set; }

			public ResourceRecord(string address, AsyncOperationHandle handle)
			{
				Address = address;
				Handle = handle;
				RefCount = 1; // ��ʼ����ʱ���ü���Ϊ1
				InstanceObjects = new List<UnityEngine.Object>();
			}

			/// <summary>
			/// ���ʵ�������󵽼�¼
			/// </summary>
			public void AddInstance(UnityEngine.Object instance)
			{
				if (instance != null && !InstanceObjects.Contains(instance))
				{
					InstanceObjects.Add(instance);
				}
			}

			/// <summary>
			/// �Ӽ�¼���Ƴ�ʵ��������
			/// </summary>
			public bool RemoveInstance(UnityEngine.Object instance)
			{
				return InstanceObjects.Remove(instance);
			}

			/// <summary>
			/// ��������ʵ��������
			/// </summary>
			public void DestroyAllInstances()
			{
				foreach (var obj in InstanceObjects)
				{
					if (obj == null) continue;

					// ����GameObject��������Դ����������ʱ/Editor��
					if (obj is GameObject go)
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							GameObject.DestroyImmediate(go); // Editor������ʱ����������
						else
#endif
							GameObject.Destroy(go); // ����ʱ���������٣����������ڣ�
					}
					else
					{
#if UNITY_EDITOR
						if (!EditorApplication.isPlaying)
							UnityEngine.Object.DestroyImmediate(obj);
						else
#endif
							UnityEngine.Object.Destroy(obj);
					}

					ArchLog.Debug($"����ʵ����{obj.name}����ԴAddress��{Address}��");
				}
				InstanceObjects.Clear();
			}

			/// <summary>
			/// �ͷ�Handle������Handle��Чʱ��
			/// </summary>
			public void ReleaseHandle()
			{
				if (Handle.IsValid())
				{
					Addressables.Release(Handle);
					ArchLog.Debug($"�ͷ���ԴHandle��{Address}");
				}
			}
		}

		/// <summary>
		/// ��Դ��¼�ֵ䣺Key=Address��Value=��Դ��¼���̰߳�ȫ��
		/// </summary>
		private static readonly Dictionary<string, ResourceRecord> _resourceRecords = new Dictionary<string, ResourceRecord>();

		/// <summary>
		/// �����󣺱���_resourceRecords���̰߳�ȫ����
		/// </summary>
		private static readonly object _recordLock = new object();

		// �Ƴ�ԭ�������_loadingHandles����_resourceRecords�����
		// private static readonly Dictionary<string, AsyncOperationHandle> _loadingHandles = new Dictionary<string, AsyncOperationHandle>();
		#endregion

	}
}
