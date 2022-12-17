using System;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RaftMMO.Network.SerializableData
{
    [System.Serializable()]
    public class MessagePlayerUpdateClone
    {
        public int x;

        public int y;

        public int z;

        public int q;

        public int w;

        public byte c;

        public int vx;

        public int vy;

        public int vz;

        public int vw;

        public bool r;

        public bool g;

        public bool i;

        public bool t;

        public bool h;

        public bool k;

        public bool l;

        public bool a;

        public int p;

        public string[] anim_triggers;

        [XmlIgnore]
        public Vector3 Position
        {
            get
            {
                return new Vector3((float)x / 100f, (float)y / 100f, (float)z / 100f);
            }
            set
            {
                x = (int)(value.x * 100f);
                y = (int)(value.y * 100f);
                z = (int)(value.z * 100f);
            }
        }

        [XmlIgnore]
        public float RotationX
        {
            get
            {
                return (float)q / 100f;
            }
            set
            {
                q = (int)(value * 100f);
            }
        }

        [XmlIgnore]
        public float RotationY
        {
            get
            {
                return (float)w / 100f;
            }
            set
            {
                w = (int)(value * 100f);
            }
        }

        [XmlIgnore]
        public ControllerType ControllerType
        {
            get
            {
                return (ControllerType)c;
            }
            set
            {
                c = (byte)value;
            }
        }

        [XmlIgnore]
        public Vector4 AnimationVelocity
        {
            get
            {
                return new Vector4((float)vx / 100f, (float)vy / 100f, (float)vz / 100f, (float)vw / 100f);
            }
            set
            {
                vx = (int)(value.x * 100f);
                vy = (int)(value.y * 100f);
                vz = (int)(value.z * 100f);
                vw = (int)(value.w * 100f);
            }
        }

        [XmlIgnore]
        public bool RaftAsParent
        {
            get
            {
                return r;
            }
            set
            {
                r = value;
            }
        }

        [XmlIgnore]
        public bool ControllerIsGrounded
        {
            get
            {
                return g;
            }
            set
            {
                g = value;
            }
        }

        [XmlIgnore]
        public bool Anim_ItemHit
        {
            get
            {
                return i;
            }
            set
            {
                i = value;
            }
        }

        [XmlIgnore]
        public bool Anim_HookThrow
        {
            get
            {
                return t;
            }
            set
            {
                t = value;
            }
        }

        [XmlIgnore]
        public bool Anim_HookInHand
        {
            get
            {
                return h;
            }
            set
            {
                h = value;
            }
        }

        [XmlIgnore]
        public bool Anim_Crouching
        {
            get
            {
                return k;
            }
            set
            {
                k = value;
            }
        }

        [XmlIgnore]
        public bool Anim_JustLanded
        {
            get
            {
                return l;
            }
            set
            {
                l = value;
            }
        }

        [XmlIgnore]
        public bool Anim_HasAmmo
        {
            get
            {
                return a;
            }
            set
            {
                a = value;
            }
        }

        [XmlIgnore]
        public int Anim_FullBodyIndex
        {
            get
            {
                return p;
            }
            set
            {
                p = value;
            }
        }


        private global::Messages type;


        public MessagePlayerUpdateClone(global::Messages type, MonoBehaviour_Network behaviour, Network_Player playerNetwork)
        {
            this.type = type;

            PersonController personController = playerNetwork.PersonController;
            ControllerType = personController.controllerType;
            RaftAsParent = personController.HasRaftAsParent;
            ControllerIsGrounded = personController.controller.isGrounded;
            if (RaftAsParent)
            {
                Position = personController.transform.localPosition;
            }
            else
            {
                Position = personController.transform.position;
            }

            if (playerNetwork.currentModel.thirdPersonSettings.ThirdPersonState)
            {
                RotationY = playerNetwork.playerPivot.eulerAngles.y;
            }
            else
            {
                RotationY = personController.transform.eulerAngles.y;
            }

            RotationX = playerNetwork.playerPivot.eulerAngles.x;
            Anim_JustLanded = personController.justLanded;
            personController.justLanded = false;
            Animator anim = playerNetwork.Animator.anim;
            anim_triggers = playerNetwork.Animator.networkAnimTriggers.ToArray();
            playerNetwork.Animator.networkAnimTriggers.Clear();
            AnimationVelocity = new Vector4(anim.GetFloat("VelocityX"), anim.GetFloat("VelocityY"), anim.GetFloat("VelocityZ"), anim.GetFloat("VelocityW"));
            Anim_ItemHit = anim.GetBool("ItemHit");
            Anim_HookThrow = anim.GetBool("HookThrow");
            Anim_HookInHand = anim.GetBool("HookInHand");
            Anim_Crouching = anim.GetBool("Crouching");
            Anim_HasAmmo = anim.GetBool("HasAmmo");
            Anim_FullBodyIndex = anim.GetInteger("FullBodyIndex");
        }

        // for serialization
        public MessagePlayerUpdateClone(){ }

        public Message_Player_Update GetMessagePlayerUpdate(MonoBehaviour_Network behaviour, Network_Player playerNetwork)
        {
            return new Message_Player_Update(type, behaviour, playerNetwork)
            {
                x = x,
                y = y,
                z = z,
                q = q,
                w = w,
                c = c,
                vx = vx,
                vy = vy,
                vz = vz,
                vw = vw,
                r = r,
                g = g,
                i = i,
                t = t,
                h = h,
                k = k,
                l = l,
                a = a,
                p = p,

                anim_triggers = anim_triggers
            };
        }
    }
}
