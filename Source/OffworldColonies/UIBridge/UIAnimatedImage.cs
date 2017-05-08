using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UIBridge {
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Animator))]
    public class UIAnimatedImage: MonoBehaviour {
        [SerializeField]
        private List<string> _animationNames;
        public List<string> AnimationNames {
            get { return _animationNames; }
        }

        public void Play(string animationName) {
            GetComponent<Animator>().Play(animationName);
        }
    }
}
