public sealed class UserDataModel : ModelBase<UserDataModel>, IModel
{
    public string UserId { get; private set; }
    public string PlayerId { get; private set; }
    public string UserName { get; private set; }
    public long Gold { get; private set; }
    public long Energy { get; private set; }
    public long Diamond { get; private set; }

    public override void Init()
    {
        Clear();
    }

    public void Apply(PlayerBootstrapResponse response)
    {
        if (response == null)
        {
            return;
        }

        UserId = response.userId;
        PlayerId = response.playerId;
        UserName = response.username;
        Gold = response.gold;
        Energy = response.energy;
        Diamond = response.diamond;
    }

    public void Clear()
    {
        UserId = string.Empty;
        PlayerId = string.Empty;
        UserName = string.Empty;
        Gold = 0;
        Energy = 0;
        Diamond = 0;
    }
}
