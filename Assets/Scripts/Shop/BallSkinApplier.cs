using BallGame.Shop;
using UnityEngine;

namespace BallGame.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BallSkinApplier : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        private void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); BallSkinService.ApplyTo(spriteRenderer); }
        private void OnEnable() { BallSkinService.SkinChanged += HandleSkinChanged; }
        private void OnDisable() { BallSkinService.SkinChanged -= HandleSkinChanged; }
        private void HandleSkinChanged(string id) { BallSkinService.ApplyTo(spriteRenderer); }
    }
}
