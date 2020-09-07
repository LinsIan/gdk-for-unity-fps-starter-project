using System;
using System.Collections;
using Fps.Animation;
using Fps.Guns;
using Fps.PlayerControls;
using Fps.SchemaExtensions;
using Fps.UI;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine;
using UnityEngine.AI;

namespace Fps.Movement
{
    public class FpsDriver : MonoBehaviour
    {
        [Serializable]
        private struct CameraSettings
        {
            public float PitchSpeed;
            public float YawSpeed;
            public float MinPitch;
            public float MaxPitch;
        }

        [Require] private ClientMovementWriter authority;
        [Require] private ServerMovementReader serverMovement;
        [Require] private GunStateComponentWriter gunState;
        [Require] private HealthComponentReader health;
        [Require] private HealthComponentCommandSender commandSender;
        [Require] private ScoreComponentCommandSender scoreCommandSender;
        [Require] private EntityId entityId;

        private ClientMovementDriver movement;
        private ClientShooting shooting;
        private ShotRayProvider shotRayProvider;
        private FpsAnimator fpsAnimator;
        private GunManager currentGun;

        [SerializeField] private Transform pitchTransform;
        [SerializeField] private new Camera camera;

        [SerializeField]
        private CameraSettings cameraSettings = new CameraSettings
        {
            PitchSpeed = 1.0f,
            YawSpeed = 1.0f
        };

        private bool isRequestingRespawn;
        private Coroutine requestingRespawnCoroutine;

        private IControlProvider controller;
        private InGameScreenManager inGameManager;

        private void Awake()
        {
            movement = GetComponent<ClientMovementDriver>();
            shooting = GetComponent<ClientShooting>();
            shotRayProvider = GetComponent<ShotRayProvider>();
            fpsAnimator = GetComponent<FpsAnimator>();
            fpsAnimator.InitializeOwnAnimator();
            currentGun = GetComponent<GunManager>();
            controller = GetComponent<IControlProvider>();

            var uiManager = GameObject.FindGameObjectWithTag("OnScreenUI")?.GetComponent<UIManager>();
            if (uiManager == null)
            {
                throw new NullReferenceException("Was not able to find the OnScreenUI prefab in the scene.");
            }

            inGameManager = uiManager.InGameManager;
            if (inGameManager == null)
            {
                throw new NullReferenceException("Was not able to find the in-game manager in the scene.");
            }
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            serverMovement.OnForcedRotationEvent += OnForcedRotation;
            health.OnRespawnEvent += OnRespawn;
            health.OnHealthModifiedEvent += OnHealthModified;
        }



        private void Update()
        {
            if (controller.MenuPressed)
            {
                inGameManager.TryOpenSettingsMenu();
            }

            if(Input.GetKeyDown(KeyCode.Tab))
            {
                inGameManager.SetRankingScreen(true);
            }

            if(Input.GetKeyUp(KeyCode.Tab))
            {
                inGameManager.SetRankingScreen(false);
            }

            // Don't allow controls if in the menu.
            if (inGameManager.InEscapeMenu)
            {
                // Still apply physics.
                movement.ApplyMovement(Vector3.zero, transform.rotation, MovementSpeed.Run, false);
                Animations(false);
                return;
            }

            if (isRequestingRespawn)
            {
                return;
            }

            if (health.Data.Health == 0)
            {
                if (controller.RespawnPressed)
                {
                    isRequestingRespawn = true;
                    requestingRespawnCoroutine = StartCoroutine(RequestRespawn());
                }

                return;
            }

            // Movement
            var toMove = transform.rotation * controller.Movement;

            // Rotation
            var yawDelta = controller.YawDelta;
            var pitchDelta = controller.PitchDelta;

            // Events
            var shootPressed = controller.ShootPressed;
            var shootHeld = controller.ShootHeld;


            // Update the pitch speed with that of the gun if aiming.
            var yawSpeed = cameraSettings.YawSpeed;
            var pitchSpeed = cameraSettings.PitchSpeed;

            //Mediator
            var yawChange = yawDelta * yawSpeed;
            var pitchChange = pitchDelta * -pitchSpeed;
            var currentPitch = pitchTransform.transform.localEulerAngles.x;
            var newPitch = currentPitch + pitchChange;
            if (newPitch > 180)
            {
                newPitch -= 360;
            }

            var currentYaw = movement.RotatedBody.eulerAngles.y;
            var newYaw = currentYaw + yawChange;
            var rotation = Quaternion.Euler(0, newYaw, 0);

            HandleShooting(shootPressed, shootHeld);

            

            var wasGroundedBeforeMovement = movement.IsGrounded;
            movement.ApplyMovement(toMove, rotation, MovementSpeed.Run, false);
        }
        
        private void OnHealthModified(HealthModifiedInfo healthModifiedInfo)
        {
            if(healthModifiedInfo.Died)
            {
                var scoreModifier = new ScoreModifier
                {
                    Amount = PlayerSettings.PlayerScore,
                    Owner = healthModifiedInfo.Modifier.ModifierId,
                };
                scoreCommandSender.SendModifyScoreCommand(healthModifiedInfo.Modifier.ModifierId, scoreModifier);
            }
        }

        private IEnumerator RequestRespawn()
        {
            while (true)
            {
                commandSender?.SendRequestRespawnCommand(entityId, new Empty());
                yield return new WaitForSeconds(2);
            }
        }

        private void OnRespawn(Empty _)
        {
            StopCoroutine(requestingRespawnCoroutine);
            isRequestingRespawn = false;
        }

        private void HandleShooting(bool shootingPressed, bool shootingHeld)
        {
            if (shootingPressed)
            {
                shooting.BufferShot();
            }

            var isShooting = shooting.IsShooting(shootingHeld);
            if (isShooting)
            {
                FireShot(currentGun.CurrentGunSettings);
            }
        }

        private void FireShot(GunSettings gunSettings)
        {
            var gunSocket = GetComponentInChildren<GunSocket>().GunTransform;
            shooting.FireShot(gunSettings.ShotRange, gunSocket);
            shooting.InitiateCooldown(gunSettings.ShotCooldown);
        }


        private void Animations(bool isJumping)
        {
            fpsAnimator.SetAiming(gunState.Data.IsAiming);
            fpsAnimator.SetGrounded(movement.IsGrounded);
            fpsAnimator.SetMovement(transform.position, Time.deltaTime);
            fpsAnimator.SetPitch(pitchTransform.transform.localEulerAngles.x);

            if (isJumping)
            {
                fpsAnimator.Jump();
            }
        }

        private void OnForcedRotation(RotationUpdate forcedRotation)
        {
            pitchTransform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }
}
