using Fusion;
using UnityEngine;

public class Item : NetworkBehaviour    // Item�N���X��NetworkBehaviour���p�����܂�
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    public float speed = 1.0f;
    [SerializeField] private Vector3 target = Vector3.forward * 5.0f;

    // �ʒu���l�b�g���[�N�œ���
    [Networked]
    public Vector3 NetworkedPosition { get; set; }  // NetworkedPosition�v���p�e�B���`���܂�

    public override void Spawned()  // Start()�̑���BSpawned���\�b�h�́A�I�u�W�F�N�g���X�|�[�����ꂽ�Ƃ��ɌĂяo����܂�
    {
        // �����ʒu��ۑ�
        startPosition = transform.position;
        endPosition = startPosition + target;

        // StateAuthority�݂̂��ʒu�𐧌�
        if (Object.HasStateAuthority)
        {
            NetworkedPosition = startPosition;
        }
        else
        {
            // �N���C�A���g�͑����ɓ����ʒu�Ɉړ�
            transform.position = NetworkedPosition;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // t��0�`1�̊Ԃ���������
            float t = Mathf.PingPong(Runner.SimulationTime * speed, 1.0f);
            // ���`��Ԃňʒu���X�V
            NetworkedPosition = Vector3.Lerp(startPosition, endPosition, t);
        }

        // ���ׂẴN���C�A���g�œ����ʒu�Ɉړ�
        transform.position = NetworkedPosition;
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Item caught by {other.name}");

        // �A�C�e�����L���b�`�����Ƃ��̏���
        if (other.TryGetComponent(out ItemCatcher itemCatcher))
        {
            // �A�C�e���L���b�`���[�̃C�x���g���Ăяo��
            itemCatcher.ItemCaught?.Invoke(this, other.GetComponent<PlayerAvatar>());
            // �A�C�e�����폜
            Runner.Despawn(Object);
        }
    }
}
