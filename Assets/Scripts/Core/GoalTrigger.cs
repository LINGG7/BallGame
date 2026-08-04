using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoalTrigger : MonoBehaviour
    {
        [SerializeField] private string goalId;

        private readonly HashSet<BallController> ballsInGoal = new HashSet<BallController>();
        private GameManager gameManager;

        public string GoalId => goalId;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Initialize(GameManager manager)
        {
            gameManager = manager;
        }

        public void ResetTrackedBalls()
        {
            ballsInGoal.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            AddBall(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            AddBall(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            BallController ball = other.GetComponentInParent<BallController>();
            if (ball == null)
            {
                return;
            }

            if (ballsInGoal.Remove(ball))
            {
                gameManager?.UnregisterBallFromGoal(this, ball);
            }
        }

        private void AddBall(Collider2D other)
        {
            if (gameManager == null || !gameManager.IsSimulating)
            {
                return;
            }

            BallController ball = other.GetComponentInParent<BallController>();
            if (ball == null)
            {
                return;
            }

            if (ballsInGoal.Add(ball))
            {
                gameManager.RegisterBallInGoal(this, ball);
            }
        }
    }
}
