using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{

    public interface IModelSource
    {
        ICodeGenSpecs CodeGenSpecs { get; }
        IBehaviors Behaviors { get; }
        IData Data { get; }
        IDimensions Dimensions { get; }
        IEnumerable<IMacro> Macros { get; }
        IModel Model { get; }
        IModelUnits ModelUnits { get; }
        ISimSpecs SimSpecs { get; }
        IModelStyle Style { get; }
    }

    public interface ICodeGenSpecs
    {
        string ClassName { get; }
        string NameSpace { get; }
    }

    public interface IBehaviors
    {
        IStockBehavior Stock { get; }
        IFlowBehavior Flow { get; }
        bool NonNegative { get; }
    }

    public interface IStockBehavior 
    {
        bool Non_Negative { get; }
    }

    public interface IFlowBehavior 
    {
        bool Non_Negative { get; }
    }

    public interface IData
    {

    }

    public interface IDimensions
    {

    }

    public interface IMacro
    {
        string Namespace { get; }
        string Name { get; }
        MacroFilter Filter { get; }
        MacroApplyTo ApplyTo { get; }
        ISimSpecs SimSpecs { get; }
        IVariables Variables { get; }
        IViews Views { get; }
        string Doc { get; }
        string Equation { get; }
        IFormat Format { get; }
        List<string> Parameters { get; }
    }

    public enum MacroFilter { Stock, Flow }
    public enum MacroApplyTo { Inflows, Outflows, Upstream, Downstream }

    public interface IVariables
    {
        IEnumerable<IAux> Auxes { get; }
        IEnumerable<IFlow> Flows { get; }
        IEnumerable<IGraphicalFunction> GraphicalFunctions { get; }
        IEnumerable<IGroup> Groups { get; }
        IEnumerable<IModule> Modules { get; }
        IEnumerable<IStock> Stocks { get; }
    }

    public interface IModule
    {
    }

    public interface IGroup
    {
    }

    public interface IViews
    {
    }

    public interface IModel
    {
        string Name { get; }
        List<IStock> Stocks { get; }
        List<IFlow> Flows { get; }
        List<IAux> Auxes { get; }

    }

    public interface IModelUnits
    {

    }

    public interface ISimSpecs
    {
        double Start { get; }
        double Stop { get; }
        string TimeUnits { get; }
        string Method { get; }
        double Pause { get; }
        double DeltaTime { get; }
        RunSpecs Run { get; }
    }

    public interface IModelStyle
    {

    }

    public interface IStock
    {
        string Name { get; }
        string VariableName { get; }
        string Equation { get; }
        string Documentation { get; }
        string Dimension { get; }
        IEventPoster EventPoster { get; }
        IGraphicalFunction GraphicalFunction { get; }
        string MathML { get; }
        List<string> Inflows { get; }
        List<string> Outflows { get; }
        //string NonNegative { get; }
        IRange Range { get; }
        IScale Scale { get; }
        string Units { get; }
        AccessType Access { get; }

        string Queue { get; }
        string Conveyor { get; }
    }

    public interface IFlow
    {
        string Dimension { get; }
        string Name { get; }
        string Equation { get; }
        string Documentation { get; }
        IEventPoster EventPoster { get; }
        IGraphicalFunction GraphicalFunction { get; }
        string MathML { get; }
        IRange Range { get; }
        IScale Scale { get; }
        string Units { get; }
        //string Leak { get; }
        //string LeakIntegers { get; }
        double Multiplier { get; }
        //string NonNegative { get; }ative = false;
        //string Overflow { get; }
        AccessType Access { get; }
    }

    public interface IAux
    {
        string Name { get; }
        string Dimension { get; }
        string Documentation { get; }
        string Element { get; }
        string Equation { get; }
        IEventPoster EventPoster { get; }
        IFormat Format { get; }
        IGraphicalFunction GraphicalFunction { get; }
        string MathML { get; }
        IRange Range { get; }
        IScale Scale { get; }
        string Units { get; }
        AccessType Access { get; }
    }

    public interface IScale
    {
        double Min { get; }
        double Max { get; }
        bool Auto { get; }
        string Group { get; }
    }

    public interface IRange
    {
        double Min { get; }
        double Max { get; }    
    }

    public interface IFormat
    {
        double Precision { get; }
        double ScaleBy { get; }
        bool Delimit000s { get; }
        DisplayAs DisplayAs { get; }
    }

    public interface IGraphicalFunction
    {
        GraphicalFunctionType InterpolationType { get; }
        double[] XPoints { get; }
        double[] YPoints { get; }
        string Name { get; }
    }

    public interface IEventPoster
    {
        double Min { get; }
        double Max { get; }
        IEnumerable<IEventPosterThreshold> Thresholds { get; }
    }

    public interface IEventPosterThreshold
    {
        IEnumerable<IEventPosterThresholdEvent> Events { get; }
        double Value { get; }
        EventPosterThresholdDirection Direction { get; }
        EventPosterThresholdRepeat Repeat { get; }
    }

    public interface IEventPosterThresholdEvent
    {
        object[] Items { get; }

        EventPosterThresholdEventSimAction SimAction { get; }
    }

    public enum AccessType { Input, Output, Both }
    public enum DisplayAs { Number, Currency, Percent }
    public enum RunSpecs { All, Group, Module }
    public enum GraphicalFunctionType { SmoothBounded, Smooth, Discrete }
    public enum EventPosterThresholdDirection { Increasing , Decreasing }
    public enum EventPosterThresholdRepeat { Each, Once, OnceEver }
    public enum EventPosterThresholdEventSimAction { Pause, Stop, Message }
}
