using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBase;

public static partial class Prefabs
{
    //factory create objs? 
    public static GameObject CreateBullet(Scene owner, Sprite sprite, Vector2 position) { 
        GameObject bullet = CreateSolid(owner, sprite, position);
        bullet.UpdateLayer(Layer.EnemyAtk);
        //to be updated to use a bullet component instead of a hitbo
        bullet.AddComponent(new Bullet(bullet));

        return bullet;  
    }

    public static GameObject CreatePlayer(Scene owner, Sprite sprite, Vector2 position) { 
        GameObject p = CreateSolid(owner, sprite, position);
        p.AddComponent(new PlayerInputComponent(p));
        p.AddComponent(new PersistenceComponent(p, "player", "playerr"));
        p.UpdateLayer(Layer.Player);
        p.AddComponent(new DamageComponent(6, p));
        return p;
    }

    public static GameObject CreateEnemy(Scene owner, Sprite sprite, Vector2 position)
    {
        GameObject e = CreateSolid(owner, sprite, position);
        e.UpdateLayer(Layer.Enemy);
        e.AddComponent(new PersistenceComponent(e, "enemy"));
        e.AddComponent(new DamageComponent(3, e));
        return e;
    }
}
