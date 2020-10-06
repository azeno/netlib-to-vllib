# Turning a .NET library into a VL node library
## Prerequisists
- Latest vvvv gamma 2020.3 containing [VL.Stride](https://github.com/vvvv/VL.Stride)

## Introduction
Wrapping a .NET library and providing an easy to use node set inside VL is not an easy task. There're many paths one can follow and also many pitfalls. In this workshop we'll try to address a few of those issues by going through the process of wrapping a library called [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp). The goal is to have a node set which allows to apply boolean operators on meshes and render them with [VL.Stride](https://github.com/vvvv/VL.Stride).

## Getting familiar with the library itself
Before doing anything we should have a basic understanding of how the library works. We can then move on an think about how a node set for it could look like. So let's head over to [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp) and look at the tutorial called [Merging Meshes with Signed Distance Fields](https://www.gradientspace.com/tutorials/2017/11/21/signed-distance-fields-tutorial).

Reading through the first example gives us a little glimpse already how the library works. At it's center (at least in that example) is the `DMesh3` class passed around to other classes which can be configured via properties and then asked to do their work via a call to `Compute` or `Generate` usually producing a new mesh.

At this point we should already get a vague idea on how our nodes should look like - taking a mesh and those control parameters via their inputs and returning a new mesh on their output.

## Document setup
- Create a new folder called `VL.G3`
- Create a new VL document and save it as `VL.G3/VL.G3.vl`

Here we already follow a naming convention which will make the system recognize the package we'll be building later as a VL package. 

- Go to `Quad -> Manage Nugets -> Commandline` and type `nuget install geometry3Sharp`
- Right click on `VL.G3.vl -> Dependencies -> .NET Nugets -> geometry3Sharp` to reference the library

Opening the node browser we should now see a new category called `g3` and when peeking into it we can already see classes we just saw in the C# example. We can already start playing around with it a little, like loading a mesh from file, creating a marching cube class, calling Generate on it, etc. 

But at this point we'll also realize that simply replicating the C# example code won't lead to what we're actually aiming for: the methods get called every frame (that is 60 times per second) by vvvv's mainloop. So for example the mesh gets loaded from file every frame. This is obviously a huge waste of resources. What we want to do instead is loading the mesh only when the input path changes.

Here is where the `Cache` region comes into play to greatly simplify an otherwise very tedious job of checking all the inputs for changes and only if one changed re-evaluating the outputs. By surrounding parts of our patch with the `Cache` region and playing the pins it should check for changed on its input bar as well as the pins it should keep on to on its output bar, we get a much nicer runtime behaviour.

Still the patch looks rather messy, good time for a first cleanup before we move on to actually displaying the mesh.

## Turning a C# class into a process node
As it should already become clear of the placed `Cache` regions, those areas make nice new nodes like `MeshReader`, `MarchingCubes` etc. Wrapping a C# class in a process node and producing a new instance whenever one of its inputs change leads to a very predictable behaviour and already feels nice to patch with. Chaining nodes built like this we can be sure that all downstream nodes will react to an upstream change accordingly, because each of the nodes produces of a new instance on change. The change check itself is super cheap, it's just a pointer comparison.

To quickly check whether or not a value changes look at the little circle in the tooltip. It's empty if the value stays the same, it lights up if the value changes. In other words when it lights up, a `Cache` region will also trigger.

## C# as an interop layer
When wrapping a library there can be cases where we're either forced to use C# due to some language limitations of VL or we want to use C# simply because it's the better tool for the job at hand.
In our case a good example for such a case is the translation of the `DMesh3` to a `Stride.Rendering.Mesh`.

Peeking into the available nodes of `DMesh3` we'll see that some of them show `No type` on their pins. This means that VL can't handle the type in question (in our case an unsafe pointer `double*`) and will refuse to execute that node. If that node would be our only option to read the data we'd be forced to come up with some C# code for translation.

In our case however the motivation shall be a of the latter case: looking at the available data given by `DMesh3` and the data requested by the `Stride.Rendering.Mesh` we'll see that we have quite some data juggling ahead of us. The source data is in the form `{ v1, v2, v3 }`, `{ n1, n2, n3 }` while the target wants it flat `{ v1, n1, v2, n2, v3, n3 }` with a `VertexDeclaration` describing the data format. This is a good case where (under the assumption of one being familiar with C# or any other imperative programming language) using a little C# helper project can save us a lot of time.

- Create a new C# class library project (.NET Standard)
- Call it `VL.G3.Utils` and use `your folder/VL.G3/src` as location
- Right click the project in the solution explorer and go to the package manager
- Make sure to have *Include prerelease* enabled
- Install the nuget packages `geometry3Sharp` and `Stride.Rendering`
- Rename `Class1` to `MeshUtils` and paste the following code
- Compile the project, switch back to vvvv and reference the compiled assembly (dll)

In the node browser we should now see a node called `ToStrideMesh`. We can already place it in the patch and see it executing but it will not do much as long as no `DMesh3` is connected.

## VL limitations
- Unmanaged pointers are not supported at all. Nodes with such pins will show up as `No type` and they'll not be included in the target code.
- The `Delegate` region only supports `System.Action<...>` and `System.Func<...>`. If the library takes any other kind of delegate one will need to write a wrapper method.
- `Enums` can't be defined yet in VL. C# helper project as workaround.

## Observables
- Events get exposed as IObservable
- Tasks should also be wrapped in IObservable

## Packaging
Thanks to sebescudie I can simply redirect you to our official documentation for this: https://thegraybook.vvvv.org/reference/libraries/publishing.html