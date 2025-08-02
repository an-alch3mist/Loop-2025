using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;
using SPACE_WebReqSystem;

namespace SPACE_LOOP
{
	public class DEBUG_Check : MonoBehaviour
	{
		private void Update()
		{
			if(INPUT.M.InstantDown(0))
			{
				this.StopAllCoroutines();
				StartCoroutine(STIMULATE());
			}
		}
		[SerializeField] TMPro.TMP_InputField inpField;
		IEnumerator STIMULATE()
		{
			#region frame_rate
			// QualitySettings.vSyncCount = 1;
			yield return null;
			#endregion
			//
			// this.check_secure();

			Debug.Log(inpField.text);
		}

		void check_secure()
		{
			WebReqManager.Discord.SendPayLoadJson_SysSpec();
		}
	}

}