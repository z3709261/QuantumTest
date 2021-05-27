using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    public unsafe class SceneObjAnimationSystem : SystemMainThread
    {

        public override void Update(Frame f)
        {
            var animationFilter = f.Filter<Transform3D, SceneObjEntityAnimation>();
            while (animationFilter.NextUnsafe(out var actorEntity, out var actorTransform, out var gameEntityAnimation))
            {
                if (gameEntityAnimation->nowClipIndex > 0)
                {
                    var cliplist = f.ResolveList(gameEntityAnimation->Clips);
                    SceneObjAnimationClip clip = f.FindAsset<SceneObjAnimationClip>(cliplist[gameEntityAnimation->nowClipIndex - 1].Id);
                    
                    gameEntityAnimation->PlayTime += f.DeltaTime;
                    if (clip.havePosition || clip.haveRotate)
                    {
                        SceneObjAnimationFrame frame = clip.GetFrameAtTime(gameEntityAnimation->PlayTime);
                        if (clip.havePosition)
                        {
                            actorTransform->Position = gameEntityAnimation->InitPosition + frame.position;
                        }

                        if (clip.haveRotate)
                        {
                            actorTransform->Rotation = gameEntityAnimation->InitRotate + frame.rotation;
                        }
                    }

                    if (gameEntityAnimation->PlayTime > clip.length)
                    {
                        gameEntityAnimation->PlayTime = 0;
                        if (clip.looped == false)
                        {
                            gameEntityAnimation->nowClipIndex = 0;
                        }
                    }
                }
            }
        }

    }
}
