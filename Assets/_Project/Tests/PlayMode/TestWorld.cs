using MochiHockey.Runtime;
using UnityEngine;

namespace MochiHockey.Tests.PlayMode
{
    /// <summary>PlayMode テスト用に、重力なしのプレイヤー・餅・ご飯粒を組み立てる。</summary>
    public static class TestWorld
    {
        public static PlayerController CreatePlayer(GameBalanceConfig config, Vector3 position, Vector3 facing)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.SetActive(false);
            player.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));
            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            var controller = player.AddComponent<PlayerController>();
            controller.Config = config;
            player.SetActive(true);
            return controller;
        }

        public static MochiBall CreateBall(GameBalanceConfig config, Vector3 position)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "MochiBall";
            ball.SetActive(false);
            ball.transform.position = position;
            var body = ball.AddComponent<Rigidbody>();
            body.useGravity = false;
            var mochiBall = ball.AddComponent<MochiBall>();
            mochiBall.Config = config;
            ball.SetActive(true);
            return mochiBall;
        }

        /// <summary>スポナーが複製するご飯粒の元（非アクティブ）。</summary>
        public static MochiRice CreateRiceTemplate()
        {
            var rice = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rice.name = "MochiRiceTemplate";
            rice.SetActive(false);
            rice.transform.localScale = Vector3.one * 0.3f;
            rice.GetComponent<Collider>().isTrigger = true;
            return rice.AddComponent<MochiRice>();
        }
    }
}
