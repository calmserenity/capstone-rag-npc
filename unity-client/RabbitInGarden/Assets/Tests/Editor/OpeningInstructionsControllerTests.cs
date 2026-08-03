using NUnit.Framework;
using UnityEngine;

public class OpeningInstructionsControllerTests
{
    [Test]
    public void PopupCanShowAndDismissWithoutSceneConfiguration()
    {
        GameObject controllerObject = new GameObject("OpeningInstructionsControllerTest");
        OpeningInstructionsController controller =
            controllerObject.AddComponent<OpeningInstructionsController>();

        controller.Show();
        Assert.That(controller.IsOpen, Is.True);

        controller.Dismiss();
        Assert.That(controller.IsOpen, Is.False);

        Object.DestroyImmediate(controllerObject);
    }
}
