using System.Collections.Generic;
using Beam.Utilities;
using Funlabs;
using Photon.Bolt;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Beam;

public class CharacterRenderer : MonoBehaviour, INeedPlayerComponent
{
	public enum RAGDOLL_TYPE
	{
		Male_Ragdoll,
		Female_Ragdoll
	}

	public bool THIRD_PERSON_CHARACTER;

	public Transform CameraPosition;

	public Transform CameraRigPosition;

	public Transform WatchPosition;

	public Transform HoldingPosition;

	public Transform ProxyBodyPosition;

	[SerializeField]
	private Animator _animatorController;

	[SerializeField]
	private AnimationEventPlayer _animationEventPlayer;

	[SerializeField]
	private DynamicBone _tiePhysics;

	[SerializeField]
	private LimbIK Left_Arm;

	private float _leftHandIKWeight;

	[SerializeField]
	private AimIK _aimIK;

	private Transform _aimTarget;

	private Vector3 _aimTargetSmooth;

	private float armCameraFollowSmoothness = 15f;

	public Transform watchTargetParent;

	[SerializeField]
	private Transform _aimTargetTransform;

	[FormerlySerializedAs("_characterRenderers")]
	[SerializeField]
	private List<Renderer> _renderers;

	[FormerlySerializedAs("_characterHeadRenderers")]
	[SerializeField]
	private Renderer[] _headRenderers;

	[SerializeField]
	private ArmEffectBase[] _armEffects;

	[SerializeField]
	private RAGDOLL_TYPE _playerRagdollType;

	[SerializeField]
	private RendererPlayerStatusEffectView _sunburnEffect;

	[SerializeField]
	private GameObject _hatEasy;

	[SerializeField]
	private GameObject _hatHard;

	private IPlayer _player;

	public Animator AnimController => _animatorController;

	public AnimationEventPlayer AnimationEventPlayer => _animationEventPlayer;

	public RAGDOLL_TYPE PlayerRagdollType => _playerRagdollType;

	public RendererPlayerStatusEffectView SunburnEffect => _sunburnEffect;

	IPlayer INeedPlayerComponent.Player => _player;

	private void Start()
	{
		if (Game.State == GameState.MAIN_MENU)
		{
			_animatorController.SetTrigger("Menu_Pose");
			_animatorController.updateMode = AnimatorUpdateMode.UnscaledTime;
			Object.Destroy(_aimIK);
			Object.Destroy(Left_Arm);
			Object.Destroy(_tiePhysics);
			Object.Destroy(this);
			return;
		}
		if (_tiePhysics != null)
		{
			if (THIRD_PERSON_CHARACTER)
			{
				_tiePhysics.enabled = true;
			}
			else
			{
				_tiePhysics.enabled = false;
			}
		}
		SetDifficultyHatRenderers();
	}

	public void SetPlayer(IPlayer player)
	{
		_player = player;
		_animationEventPlayer.SetPlayer(player);
		for (int i = 0; i < _armEffects.Length; i++)
		{
			_armEffects[i].SetPlayer(player);
		}
		_aimTarget = _player.Character.AimTarget;
		SetupWatchIK();
	}

	private void SetDifficultyHatRenderers()
	{
		bool active = PlayerUtilities.GetServerGameDifficulty() == EGameDifficultyMode.Creative;
		bool active2 = PlayerUtilities.GetServerGameDifficulty() == EGameDifficultyMode.Hard;
		if (_hatEasy != null)
		{
			_hatEasy.SetActive(active);
		}
		if (_hatHard != null)
		{
			_hatHard.SetActive(active2);
		}
	}

	public void SetSpyglassRenderers(bool shadowsOnly = false)
	{
		for (int i = 0; i < _renderers.Count; i++)
		{
			_renderers[i].shadowCastingMode = ((!shadowsOnly) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
			_renderers[i].receiveShadows = true;
		}
	}

	public void SetHeadRenderers(bool enabled, bool shadowsOnly = false)
	{
		for (int i = 0; i < _headRenderers.Length; i++)
		{
			_headRenderers[i].enabled = enabled;
			_headRenderers[i].shadowCastingMode = ((!shadowsOnly) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
			_headRenderers[i].receiveShadows = enabled;
		}
	}

	public void SetArmEffectActive(bool enabled)
	{
		for (int i = 0; i < _armEffects.Length; i++)
		{
			_armEffects[i].SetActive(enabled);
		}
	}

	public void SetRenderers(bool enabled)
	{
		for (int i = 0; i < _renderers.Count; i++)
		{
			_renderers[i].enabled = enabled;
		}
	}

	private void SetupWatchIK()
	{
		Transform transform = new GameObject("Watch_IK_Target").transform;
		transform.transform.parent = watchTargetParent;
		transform.localPosition = new Vector3(-0.2382f, -0.0417f, 0.118f);
		transform.localRotation = Quaternion.Euler(new Vector3(52.206f, -181.599f, -6.622f));
		Left_Arm.solver.target = transform;
	}

	private void Update()
	{
		if (_player.IsNullOrDestroyed())
		{
			return;
		}
		SetupAnimatorMasks();
		if (_player.IsOwner)
		{
			_animatorController.SetBool("HoldingItem", _player.Holder.CheckCurrent());
			_animatorController.SetFloat("HoldingItemFloat", (_animatorController.GetBool("HoldingItem") && !THIRD_PERSON_CHARACTER) ? 1 : 0);
		}
		if (_animatorController.GetFloat("watch") > 0.1f)
		{
			if (!THIRD_PERSON_CHARACTER && _leftHandIKWeight <= 1f)
			{
				_leftHandIKWeight += 3.5f * Time.deltaTime;
			}
		}
		else if (_leftHandIKWeight > 0f)
		{
			_leftHandIKWeight -= 3.5f * Time.deltaTime;
		}
		Left_Arm.solver.IKPositionWeight = _leftHandIKWeight;
		Left_Arm.solver.IKRotationWeight = _leftHandIKWeight;
		if (_player.Character.PlayingEventAnimation || _player.Movement.ClimbingLedge || _player.Movement.ClimbingLadder || _player.Movement.Vehicle != null || _player.GameInput.FireLighter.IsLightingFire)
		{
			_aimIK.solver.bones[0].weight = 0f;
			_aimIK.solver.bones[1].weight = 0f;
		}
		else if (THIRD_PERSON_CHARACTER)
		{
			_aimIK.solver.bones[0].weight = 0.1f;
			_aimIK.solver.bones[1].weight = 0.1f;
		}
		else
		{
			_aimIK.solver.bones[0].weight = 1f;
			_aimIK.solver.bones[1].weight = 1f;
		}
	}

	private void LateUpdate()
	{
		if (!_player.IsNullOrDestroyed())
		{
			if (_player.Movement.InWater)
			{
				_aimTargetTransform.localPosition = new Vector3(_aimTargetTransform.localPosition.x, 4.35f, _aimTargetTransform.localPosition.z);
			}
			else
			{
				_aimTargetTransform.localPosition = new Vector3(_aimTargetTransform.localPosition.x, 0f, _aimTargetTransform.localPosition.z);
			}
			_aimTargetSmooth = Vector3.Lerp(_aimTargetSmooth, _player.PlayerCamera.transform.position + _player.PlayerCamera.transform.up * 3.8f + _player.PlayerCamera.transform.forward * 20f, Time.deltaTime * armCameraFollowSmoothness);
			_aimTarget.transform.position = _aimTargetSmooth;
			_aimIK.solver.transform.LookAt(_aimTarget);
			_aimIK.solver.axis = _aimIK.solver.transform.InverseTransformDirection(transform.forward);
			_aimIK.solver.IKPosition = _aimTarget.position;
		}
	}

	private void SetupAnimatorMasks()
	{
		if (THIRD_PERSON_CHARACTER)
		{
			_animatorController.SetLayerWeight(1, 0f);
			_animatorController.SetLayerWeight(2, 0f);
			_animatorController.SetLayerWeight(3, 0f);
			_animatorController.SetLayerWeight(4, 1f);
			_animatorController.SetLayerWeight(5, 1f);
			_animatorController.SetLayerWeight(6, 1f);
			_animatorController.SetLayerWeight(7, 1f);
		}
		else
		{
			_animatorController.SetLayerWeight(4, 0f);
			_animatorController.SetLayerWeight(5, 0f);
			_animatorController.SetLayerWeight(6, 0f);
			_animatorController.SetLayerWeight(7, 0f);
			_animatorController.SetLayerWeight(1, 1f);
			_animatorController.SetLayerWeight(2, 1f);
			_animatorController.SetLayerWeight(3, 1f);
		}
		if (_player.Character.PlayingEventAnimation || _player.Movement.ClimbingLadder || _player.Movement.ClimbingLedge)
		{
			_animatorController.SetLayerWeight(1, 0f);
			_animatorController.SetLayerWeight(2, 0f);
			_animatorController.SetLayerWeight(3, 0f);
			_animatorController.SetLayerWeight(4, 0f);
			_animatorController.SetLayerWeight(5, 0f);
			_animatorController.SetLayerWeight(6, 0f);
			_animatorController.SetLayerWeight(7, 0f);
		}
	}

	public void InstantiatePlayerRagdoll(Vector3 position)
	{
		MultiplayerMng.Instantiate<InteractiveObject_PLAYER_RAGDOLL>((_playerRagdollType == RAGDOLL_TYPE.Male_Ragdoll) ? BoltPrefabs.Player_Ragdoll_Male : BoltPrefabs.Player_Ragdoll_Female, new MultiplayerTokens.SaveablePrefab
		{
			Id = MiniGuid.PLAYER_RAGDOLL(_player)
		}).transform.position = position;
	}
}
