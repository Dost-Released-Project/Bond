using Bond.Embark;
using Bond.UI.PartySelection;
using Bond.UI.RoleReactionEditor;
using VContainer;
using VContainer.Unity;

namespace Ha
{
    public class TestScope : LifetimeScope
    {
        public Ha.Test t;
        public RoleReactionEditorController roleReactionEditorController;
        public PartySelectionController partySelectionController;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PartyManager>(Lifetime.Scoped);
            builder.Register<StageCoach>(Lifetime.Scoped);
            builder.RegisterComponent(t);
            builder.RegisterComponent(roleReactionEditorController);
            builder.RegisterComponent(partySelectionController);
        }
    }
}
