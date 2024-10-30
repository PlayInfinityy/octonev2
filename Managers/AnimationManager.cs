using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace octonev2.Managers
{
    public static class AnimationManager
    {
        public static async Task PlayFadeInAnimation(Form form)
        {
            form.Opacity = 0;
            while (form.Opacity < 1)
            {
                await Task.Delay(10);
                form.Opacity += 0.05;
            }
        }

        public static async Task PlayFadeOutAnimation(Form form)
        {
            while (form.Opacity > 0)
            {
                await Task.Delay(10);
                form.Opacity -= 0.05;
            }
        }
    }
}
