// A logical robot link may contain several mesh Parts. Inspect every public geometry child.
using System;using System.Linq;using System.Collections.Generic;
using ABB.Robotics.RobotStudio.Stations;
namespace Q7n4.Portable014m {
 public static class LinkMeshParts014 {
  public static Part[] All(GraphicComponent link){var seen=new HashSet<Guid>();var result=new List<Part>();
   void Visit(GraphicComponent node){if(!seen.Add(Guid.Parse(node.UniqueId)))throw new InvalidOperationException("Duplicate or cyclic robot geometry child: "+node.Name);if(node is Part part){result.Add(part);return;}if(node is IHasGraphicComponents group){var children=group.GraphicComponents.ToArray();if(children.Length==0)throw new InvalidOperationException("Empty logical link geometry: "+node.Name);foreach(var child in children)Visit(child);return;}throw new InvalidOperationException("Unsupported robot geometry node: "+node.GetType().FullName);}
   Visit(link);if(result.Count==0)throw new InvalidOperationException("A logical link must have actual Part geometry: "+link.Name);return result.ToArray();
  }
 }
}
