﻿namespace Text2Diagram_Backend.Features.Sequence.Components;
public class CriticalBlock : SequenceElement
{
    public string Title { get; set; } = string.Empty; // e.g. "Establish a connection to the DB"
    public List<SequenceElement> Body { get; set; } = new();
    public List<OptionBlock> Options { get; set; } = new();
}
