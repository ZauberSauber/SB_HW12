using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public float speed = 5;
    public float rotationSpeed = 20;
    public float jumpForce = 8;
    public float gravity = 20;
    [Header("Безопасная высота с которой можно упасть")]
    public float maxSaveHeight = 5;
    [Header("Анимация прыжка")]
    public AnimationClip jumpAnimation;

    [Header("Анимация ближней атаки")]
    public AnimationClip closeAttack;
    [Header("Анимация средней атаки")]
    public AnimationClip midAttack;
    [Header("Анимация дальней атаки")]
    public AnimationClip rangedAttack;

    private Animator _animator;

    private CharacterController _characterController;
    private Vector3 _moveDirection = Vector3.zero;

    private bool _isDead;
    private bool _isFlying;
    private bool _isPreparingForJumping;
    private bool _isAttacking;
    private bool _canJump;
    private float _jumpPrepareDuration;    // время подготовки к прыжку
    private float _maxJumpElevation;    // максимальная высота полёта
    private float _landingElevation;    // высота на которой произошло приземление

    private const float DefaultPrepareJumpTime = 0.2f;
    private const float DefaultAttackTime = 0.5f;


    private void Start() {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        _jumpPrepareDuration = jumpAnimation ? jumpAnimation.length : DefaultPrepareJumpTime;
        _maxJumpElevation = transform.position.y;
    }

    
    private void Update() {
        if (!_animator || _isDead) {
            return;
        }
        
        if (!CheckGroundByRay()) {
            _isFlying = true;
            _canJump = false;
        }

        if (_isFlying) {
            _maxJumpElevation = Mathf.Max(_maxJumpElevation, transform.position.y);
        }
        
        _animator.SetBool("IsFlying", _isFlying);
        
        Attack();
    }

    private void FixedUpdate() {
        if (!_isAttacking) {
            MovePlayer();
        }
    }


    private void MovePlayer() {
        if (_characterController.isGrounded) {
            if (_isFlying) {
                _isFlying = false;
                _canJump = false;
                
                _animator.SetBool("IsFlying", _isFlying);
                _landingElevation = transform.position.y;
                
                // Проверка на смерть от падения
                if (Mathf.Abs(_landingElevation - _maxJumpElevation) > maxSaveHeight) {
                    _animator.SetTrigger("FallDeath");
                    _isDead = true;
                    return;
                }
                
                _animator.SetTrigger("Landing");
            }
            
            float move = Input.GetAxis("Vertical");
            
            _animator.SetFloat("Movement", -move);

            _moveDirection = new Vector3(0.0f, 0.0f, -move);
            _moveDirection = transform.TransformDirection(_moveDirection);
            _moveDirection *= speed;

            PlayerJump();

            RotatePlayer();
        }
        
        _moveDirection.y -= (gravity * Time.deltaTime);
                
        _characterController.Move(_moveDirection * Time.deltaTime);
    }

    /// <summary>
    /// Поворачивает игрока по оси У
    /// </summary>
    private void RotatePlayer() {
        float rotate = Input.GetAxis("Horizontal");
        _animator.SetFloat("Rotation", rotate);
        transform.Rotate(0,  rotate * rotationSpeed * Time.deltaTime, 0);
    }

    /// <summary>
    /// Проигрывает анимации атаки
    /// </summary>
    private void Attack() {
        if (_isFlying) {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            _animator.SetTrigger("Attack");
            StartCoroutine(FreezeByAttack(closeAttack));
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            _animator.SetTrigger("CloseAttack");
            StartCoroutine(FreezeByAttack(midAttack));
        } 
        else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            _animator.SetTrigger("RangedAttack");
            StartCoroutine(FreezeByAttack(rangedAttack));
        }
    }

   /// <summary>
   /// Осущевствляет прыжок игрока
   /// </summary>
    private void PlayerJump() {
        if (_canJump) {
            _moveDirection.y = jumpForce;
        } 
        else if (Input.GetButton("Jump") && !_isPreparingForJumping) {
            _isPreparingForJumping = true;
            StartCoroutine(PrepareJump());
        }
    }

    /// <summary>
    /// Дополнительная проверка на заземление, т.к. завязывание на characterController.isGrounded
    /// приводит к проигрыванию анимации полёта на ровном месте
    /// </summary>
    /// <returns></returns>
    private bool CheckGroundByRay() {
        if (Physics.Raycast(transform.position, Vector3.down, 0.2f)) {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Подготовка к прыжку
    /// </summary>
    /// <returns></returns>
    IEnumerator PrepareJump() {
        _animator.SetTrigger("Jump");
        
        yield return new WaitForSeconds(_jumpPrepareDuration);

        _maxJumpElevation = transform.position.y;
        _canJump = true;
        _isPreparingForJumping = false;
    }

    /// <summary>
    /// Устанавливает статус isAttacking на время проигрывания анимации
    /// </summary>
    /// <param name="clip"></param>
    /// <returns></returns>
    IEnumerator FreezeByAttack(AnimationClip clip) {
        float time = clip ? clip.length : DefaultAttackTime;
        
        _isAttacking = true;
        
        yield return new WaitForSeconds(time);

        _isAttacking = false;
    }
}
