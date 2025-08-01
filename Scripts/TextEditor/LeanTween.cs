using System;
using System.Collections;
using UnityEngine;

namespace GptDeepResearch
{
	public class LeanTween : MonoBehaviour
	{
		private static LeanTween _instance;
		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}

		public static Tween scale(GameObject target, Vector3 to, float duration)
		{
			EnsureInstanceExists();
			Tween t = new Tween(target.transform, Tween.TweenType.Scale, to, duration);
			_instance.StartCoroutine(t.Run());
			return t;
		}

		private static void EnsureInstanceExists()
		{
			if (_instance == null)
			{
				GameObject go = new GameObject("HardCodedLeanTween");
				DontDestroyOnLoad(go);
				_instance = go.AddComponent<LeanTween>();
			}
		}

		public class Tween
		{
			public enum TweenType { Scale }
			private Transform _transform;
			private TweenType _type;
			private Vector3 _to;
			private float _duration;
			private Func<float, float> _easing = Easing.Linear;
			private int _loops = 0;

			public Tween(Transform transform, TweenType type, Vector3 to, float duration)
			{
				_transform = transform;
				_type = type;
				_to = to;
				_duration = duration;
			}

			public Tween setEaseInOutSine()
			{
				_easing = Easing.InOutSine;
				return this;
			}

			public Tween setLoopPingPong(int loops)
			{
				_loops = loops;
				return this;
			}

			public IEnumerator Run()
			{
				Vector3 from = _transform.localScale;
				int count = 0;
				bool reverse = false;
				do
				{
					float elapsed = 0f;
					while (elapsed < _duration)
					{
						elapsed += Time.deltaTime;
						float t = Mathf.Clamp01(elapsed / _duration);
						float eased = _easing(t);
						_transform.localScale = Vector3.Lerp(from, _to, eased);
						yield return null;
					}

					// Swap for ping-pong
					Vector3 temp = from;
					from = _to;
					_to = temp;
					count++;
					reverse = !reverse;

				} while (count <= _loops);
			}

			private static class Easing
			{
				public static float Linear(float t)
				{
					return t;
				}

				public static float InOutSine(float t)
				{
					return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
				}
			}
		}
	}

}