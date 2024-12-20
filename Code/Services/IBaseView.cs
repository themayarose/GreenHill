namespace GreenHill.Services;

public interface IBaseView<T> where T : BaseViewModel? {
    public T ViewModel { get; }
}