namespace OffworldColonies.Part {
    public interface ISharedResourceProvider {
        double RequestResource(int resID, double demand);
    }
}