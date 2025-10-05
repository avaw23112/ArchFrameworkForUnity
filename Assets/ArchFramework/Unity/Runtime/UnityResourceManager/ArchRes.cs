using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Arch.Resource
{
	public static class ArchRes
	{
		private static IResProvider _provider;

		public static void SetProvider(IResProvider provider)
		{
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public static UniTask InitializeAsync()
		{
			if (_provider == null) throw new InvalidOperationException("Provider not set.");
			return _provider.InitializeAsync();
		}

		// ·���޹�
		public static UniTask<T> LoadAsync<T>(string name) where T : class =>
			_provider.LoadAsync<T>(name);

		// �ֲ���أ�label + name������ UnityResProvider ֧��
		public static async UniTask<T> LoadAsync<T>(string label, string name) where T : class
		{
			if (_provider is UnityResProvider u) return await u.LoadAsync<T>(label, name);
			throw new NotSupportedException("Current provider does not support layered loading.");
		}

		public static UniTask<IEnumerable<T>> LoadAllByLabelAsync<T>(string label) where T : class
			=>
			_provider.LoadAllByLabelAsync<T>(label);

		public static IEnumerable<T> LoadAllByLabel<T>(string label) where T : class =>
			_provider.LoadAllByLabel<T>(label);

		public static void Release(string name) => _provider?.Release(name);

		// �ֲ�ж�أ�label + name������ UnityResProvider ֧��
		public static void Release(string label, string name)
		{
			if (_provider is UnityResProvider u) u.Release(label, name);
		}

		// ǿ��ж�أ��� UnityResProvider ֧��
		public static void ForceRelease(string name)
		{
			if (_provider is UnityResProvider u) u.ForceRelease(name);
		}

		public static void ForceRelease(string label, string name)
		{
			if (_provider is UnityResProvider u) u.ForceRelease(label, name);
		}

		public static void ForceReleaseLabel(string label)
		{
			if (_provider is UnityResProvider u) u.ForceReleaseLabel(label);
		}

		// ʵ���������٣�������������� UnityResProvider ֧��
		public static UniTask<UnityEngine.GameObject> InstantiateAsync(string name, UnityEngine.Transform parent = null)
		{
			if (_provider is UnityResProvider u) return u.InstantiateAsync(name, parent);
			throw new NotSupportedException();
		}

		public static UniTask<UnityEngine.GameObject> InstantiateAsync(string label, string name, UnityEngine.Transform parent = null)
		{
			if (_provider is UnityResProvider u) return u.InstantiateAsync(label, name, parent);
			throw new NotSupportedException();
		}

		public static void DestroyInstance(UnityEngine.Object instance)
		{
			if (_provider is UnityResProvider u) u.DestroyInstance(instance);
		}

		public static void ReleaseAll() => _provider?.ReleaseAll();
	}
}