#include "Path.h"
#include "FDrawCmdList.h"

NS_BEGIN

namespace Canvas
{
	namespace Path
	{
		FPathPoint FPathPoint::Interplate(const FPathPoint& P0, const FPathPoint& P1, float Distance)
		{
			if (Distance < P0.Distance)
				return P0;
			if (Distance > P1.Distance)
				return P1;

			float Delta = P1.Distance - P0.Distance;
			float Alpha = Delta < 1e-6f ? 0 : (Distance - P0.Distance) / Delta;
			FPathPoint Point;
			Point.Position = FPathUtility::Lerp(P0.Position, P1.Position, Alpha);
			Point.Forward = FPathUtility::Normalize(FPathUtility::Lerp(P0.Forward, P1.Forward, Alpha));
			Point.Distance = Distance;
			Point.Flag = Point_None;
			return Point;
		}

		void FPathPoly::Traingulate()
		{
			if (HasTriangulated())
				return;

			Indices.clear();
			const bool bConvex = PolyType == Poly_Convex;
			FPathUtility::Triangulate(Vertices.data(), GetSize(), Indices, 0, bConvex);
		}

		void FPathPoly::WindowClip(const FRectanglef& Rect)
		{
			const bool bHasUV = HasUvChannel();
			const bool bShouldKeepUV = bHasUV && HasTriangulated();
			const UINT NumVerts = GetSize();

			std::vector<v3dxVector2> NewVerts;
			std::vector<v3dxVector2> NewUVs;
			std::vector<UINT> NewInds;

			if (bShouldKeepUV)
			{
				const UINT NumTri = (UINT)Indices.size() / 3;
				for (UINT i = 0; i < NumTri; i++)
				{
					const UINT Ind0 = Indices[i * 3];
					const UINT Ind1 = Indices[i * 3 + 1];
					const UINT Ind2 = Indices[i * 3 + 2];
					std::vector<v3dxVector2> TriVerts = { Vertices[Ind0], Vertices[Ind1], Vertices[Ind2] };
					std::vector<v3dxVector2> TriUVs = { UVs[Ind0], UVs[Ind1], UVs[Ind2] };

					const UINT StartInd = (UINT)NewVerts.size();
					FPathUtility::SutherlandHodgmanClip(
						Rect,
						TriVerts.data(),
						3,
						NewVerts,
						TriUVs.data(),
						&NewUVs
					);
					const UINT Count = (UINT)NewVerts.size() - StartInd;
					FPathUtility::Triangulate(
						NewVerts.data() + StartInd,
						Count,
						NewInds,
						StartInd
					);
				}
			}
			else if (bHasUV)
			{
				FPathUtility::SutherlandHodgmanClip(
					Rect,
					Vertices.data(),
					NumVerts,
					NewVerts,
					UVs.data(),
					&NewUVs
				);
				FPathUtility::Triangulate(
					NewVerts.data(),
					(UINT)NewVerts.size(),
					NewInds, 
					0,
					PolyType == Poly_Convex
				);
			}
			else
			{
				FPathUtility::SutherlandHodgmanClip(
					Rect,
					Vertices.data(),
					NumVerts,
					NewVerts
				);
				FPathUtility::Triangulate(
					NewVerts.data(),
					(UINT)NewVerts.size(),
					NewInds,
					0,
					PolyType == Poly_Convex
				);
			}
			Vertices = std::move(NewVerts);
			UVs = std::move(NewUVs);
			Indices = std::move(NewInds);
		}

		FRectanglef FPathPoly::GetBounds() const
		{
			return FPathUtility::CalcBounds(Vertices.data(), GetSize());
		}

		void FPath::BeginPath()
		{
			Clear();
		}

		void FPath::PushCmd(EPathCmdType CmdType, const void* Args, UINT Count)
		{
			CmdList.push_back(CmdType);
			ASSERT(Count >= GPathCmdCoord[CmdType]);
			for (UINT i = 0; i < GPathCmdCoord[CmdType]; i++)
				Params.push_back(((float*)Args)[i]);
		}

		void FPath::EndPath(Canvas::FCanvasDrawCmdList* DrawCmdList, Canvas::FSubDrawCmd* pOutCmd)
		{
			auto pCmd = DrawCmdList->GetTopBrushDrawCmd();
			if (pCmd == nullptr)
				return;

			if (CmdList.empty() || Params.empty())
			{
				Clear();
				return;
			}

			const auto Matrix = DrawCmdList->GetCurrentMatrix();
			const auto Brush = DrawCmdList->GetCurrentBrush();
			Style = DrawCmdList->GetCurrentPathStyle();
			if (Style == nullptr)
				Style = MakeWeakRef(new FPathStyle);
			const bool UvAlongPath = Style->UvAlongPath;

			BuildPath();
			Fill();
			Stroke();

			UpdateBounds();
			if (!Bounds.IsValid())
			{
				Clear();
				return;
			}

			if (DrawCmdList->ShouldClip())
			{
				Scissor(DrawCmdList->GetCurrentClipRect());
			}
			Trianglute();

			const UINT StartInd = (UINT)pCmd->mVertices.size();
			if (pOutCmd)
			{
				pOutCmd->VertexStart = StartInd;
				pOutCmd->IndexStart = (UINT)pCmd->mIndices.size();
			}

			const FRectanglef PathBds(0, 0, PathLength, Style->PathWidth);
			const FRectanglef& BrushRect = Brush ? Brush->Rect : FRectanglef(0, 0, 1, 1);
			const v3dxVector4& Tiling = Style->PaintTiling;

			for (const auto& Poly : Polygons)
			{
				const bool bHasUV = Poly.HasUvChannel();
				const UINT NumVerts = (UINT)Poly.Vertices.size();
				const UINT NumInds = (UINT)Poly.Indices.size();
				UINT vStart = (UINT)pCmd->mVertices.size();
				for (UINT i = 0; i < NumVerts; i++)
				{
					const v3dxVector2& Position = Poly.Vertices[i];
					Canvas::FCanvasVertex vCanvas;
					vCanvas.Pos = v3dxVector3(Position.x, Position.y, 0);
					vCanvas.UV = UvAlongPath && bHasUV ?
						RemapUV(PathBds, BrushRect, Tiling, Poly.UVs[i]) :
						RemapUV(Bounds, BrushRect, Tiling, Position);
					pCmd->mVertices.push_back(vCanvas);
				}
				for (UINT i = 0; i < NumInds; i++)
					pCmd->mIndices.push_back(vStart + Poly.Indices[i]);
			}

			const UINT EndInd = (UINT)pCmd->mVertices.size();
			if (pOutCmd)
			{
				pOutCmd->Count = EndInd - StartInd;
				pOutCmd->IndexCount = (UINT)pCmd->mIndices.size() - pOutCmd->IndexStart;
			}

			if (Matrix)
			{
				for (UINT i = StartInd; i < EndInd; i++)
				{
					v3dxVector3& Pos = pCmd->mVertices[i].Pos;
					Pos = v3dxVector4(Pos.x, Pos.y, 0, 1) * (*Matrix);
				}
			}
		}

		void FPath::MoveTo(const v3dxVector2& P)
		{
			CmdList.push_back(Cmd_MoveTo);
			Params.push_back(P.x);
			Params.push_back(P.y);
		}

		void FPath::LineTo(const v3dxVector2& P)
		{
			CmdList.push_back(Cmd_LineTo);
			Params.push_back(P.x);
			Params.push_back(P.y);
		}

		void FPath::ArcTo(const v3dxVector2& P, float Radius, bool IsCCW, bool IsSmall)
		{
			if (!IsSmall && IsCCW)
				CmdList.push_back(Cmd_LCCWArcTo);
			else if (IsSmall && IsCCW)
				CmdList.push_back(Cmd_SCCWArcTo);
			else if (IsSmall && !IsCCW)
				CmdList.push_back(Cmd_SCWArcTo);
			else
				CmdList.push_back(Cmd_LCWArcTo);
			Params.push_back(P.x);
			Params.push_back(P.y);
			Params.push_back(Radius);
		}

		void FPath::Close()
		{
			CmdList.push_back(Cmd_Close);
		}

		void FPath::PushRect(const v3dxVector2& Min, const v3dxVector2& Max, float Round)
		{
			if (Round < 1.e-6f)
			{
				MoveTo(Min);
				LineTo(v3dxVector2(Max.x, Min.y));
				LineTo(Max);
				LineTo(v3dxVector2(Min.x, Max.y));
				Close();
			}
			else
			{
				const float HalfWidth = (Max.x - Min.x) * 0.5f;
				const float HalfHeight = (Max.y - Min.y) * 0.5f;
				Round = std::min(HalfWidth, std::min(HalfHeight, Round));
				MoveTo(v3dxVector2(Min.x + Round, Min.y));
				LineTo(v3dxVector2(Max.x - Round, Min.y));
				ArcTo(v3dxVector2(Max.x, Min.y + Round), Round);
				LineTo(v3dxVector2(Max.x, Max.y - Round));
				ArcTo(v3dxVector2(Max.x - Round, Max.y), Round);
				LineTo(v3dxVector2(Min.x + Round, Max.y));
				ArcTo(v3dxVector2(Min.x, Max.y - Round), Round);
				LineTo(v3dxVector2(Min.x, Min.y + Round));
				ArcTo(v3dxVector2(Min.x + Round, Min.y), Round);
				Close();
			}
		}

		void FPath::PushCircle(const v3dxVector2& Center, float Radius)
		{
			v3dxVector2 Start = Center + v3dxVector2(Radius, 0);
			MoveTo(Start);
			ArcTo(Center - v3dxVector2(Radius, 0), Radius);
			ArcTo(Start, Radius);
			Close();
		}
		
		void FPath::Clear()
		{
			Style = nullptr;
			Params.clear();
			CmdList.clear();
			Points.clear();
			Groups.clear();
			Polygons.clear();
			PathLength = 0;
			Bounds = FRectanglef(0, 0, -1, -1);
		}

		void FPath::BuildPath()
		{
			if (CmdList.empty())
				return;

			std::vector<float>::iterator Iter = Params.begin();
			v3dxVector2 Prev;
			v3dxVector2 Begin;
			FPathGroup CurGroup;
			float Distance = 0.f;

			for (auto Cmd : CmdList)
			{
				switch (Cmd)
				{
				case Cmd_Close:
				{
					LineToInternal(Prev, Begin, Distance, CurGroup);
					Prev = Begin;

					CurGroup.bLoop = true;
					if (IsValidGroup(CurGroup))
					{
						Points[CurGroup.Begin].Flag = Point_Joint;
						Points[CurGroup.End - 1].Flag = Point_Joint;
						Groups.push_back(CurGroup);
					}
					Distance = 0.f;
					CurGroup = FPathGroup();
					CurGroup.Begin = (UINT)Points.size();

				} break;
				case Cmd_MoveTo:
				{
					float x = *Iter++;
					float y = *Iter++;
					Prev = v3dxVector2(x, y);
					Begin = Prev;
					Distance = 0.f;

					if (IsValidGroup(CurGroup))
						Groups.push_back(CurGroup);
					Distance = 0.f;
					CurGroup = FPathGroup();
					CurGroup.Begin = (UINT)Points.size();

				} break;
				case Cmd_LineTo:
				{
					float x = *Iter++;
					float y = *Iter++;
					v3dxVector2 End = v3dxVector2(x, y);
					LineToInternal(Prev, End, Distance, CurGroup);
					Prev = End;

				} break;
				case Cmd_SCCWArcTo:
				case Cmd_SCWArcTo:
				case Cmd_LCCWArcTo:
				case Cmd_LCWArcTo:
				{
					float x = *Iter++;
					float y = *Iter++;
					float r = *Iter++;
					bool IsCCW = Cmd == Cmd_SCCWArcTo || Cmd == Cmd_LCCWArcTo;
					bool IsSmall = Cmd == Cmd_SCCWArcTo || Cmd == Cmd_SCWArcTo;
					v3dxVector2 End = v3dxVector2(x, y);
					ArcToInternal(Prev, End, r, IsCCW, IsSmall, Distance, CurGroup);
					Prev = End;

				} break;
				default:
					break;
				}
			}

			if (IsValidGroup(CurGroup))
				Groups.push_back(CurGroup);

			if (!Points.empty())
				PathLength = (*Points.rbegin()).Distance;

			Params.clear();
			CmdList.clear();
		}

		void FPath::Fill()
		{
			if (!Style || !Style->FillArea)
				return;

			for (auto& Group : Groups)
			{
				if (!IsValidGroup(Group) || Group.End - Group.Begin < 3 ||
					(Group.bLoop && Group.End - Group.Begin < 4))
					continue;

				FPathPoly Poly;
				for (UINT i = Group.Begin; i < Group.End; i++)
					Poly.Vertices.push_back(Points[i].Position);
				Polygons.push_back(std::move(Poly));
			}
		}

		void FPath::Stroke()
		{
			if (!Style || Style->PathWidth < 1e-6f)
				return;

			if (Style->StrokeMode == Stroke_Dot && Style->CapMode == Cap_Butt)
				return;

			const float HalfWidth = Style->PathWidth * 0.5f;
			const auto& Pattern = Style->StrokePattern;
			float Period = 0.f;
			for (float l : Pattern)
				Period += l;
			EPathStrokeMode StrokeMode = Period < 1e-6f ? Stroke_Normal : Style->StrokeMode;

			for (auto& Group : Groups)
			{
				if (!IsValidGroup(Group))
					continue;

				std::vector<FPathPoint> NewPoints;
				switch (StrokeMode)
				{
				case Stroke_Normal:
					NewPoints.insert(NewPoints.end(), Points.begin() + Group.Begin, Points.begin() + Group.End);
					break;
				case Stroke_Dash:
				{	
					const float Length = Points[Group.End - 1].Distance - Points[Group.Begin].Distance;
					float Offset = Style->StrokeOffset;
					if (Offset < 0)
					{
						UINT NumOffsetRpt = (UINT)std::ceil(-Offset / Period);
						Offset = Period * NumOffsetRpt + Offset;
					}
					UINT NumRepeat = (UINT)std::ceil((Length + Offset) / Period);
					float PatternScalar = 1.0f;
					if (Style->StrokeAutoAlign)
					{
						if (NumRepeat % 2 && (UINT)Pattern.size() % 2)
							NumRepeat += 1;
						PatternScalar = Length / (NumRepeat * Period);
						Offset *= PatternScalar;
						if (Offset > 0)
							NumRepeat += 1;
					}

					const UINT NumMaxClips = NumRepeat * (UINT)Pattern.size();
					float PrevDist = -Offset;
					bool IsVisible = true;
					for (UINT i = 0, PointInd = Group.Begin; i < NumMaxClips; i++)
					{
						float NextDist = PrevDist + Pattern[i % Pattern.size()] * PatternScalar;
						if (IsVisible && NextDist > 0)
						{
							while (PointInd < Group.End && Points[PointInd].Distance <= PrevDist)
								PointInd++;

							if (PointInd == Group.End)
								break;

							if (PointInd != Group.Begin)
							{
								FPathPoint StartPoint = FPathPoint::Interplate(Points[PointInd - 1], Points[PointInd], PrevDist);
								StartPoint.Flag = Point_Start;
								NewPoints.push_back(StartPoint);
							}

							while (PointInd < Group.End && Points[PointInd].Distance < NextDist)
								NewPoints.push_back(Points[PointInd++]);

							if (PointInd == Group.End)
								break;

							FPathPoint EndPoint = FPathPoint::Interplate(Points[PointInd - 1], Points[PointInd], NextDist);
							EndPoint.Flag = Point_End;
							NewPoints.push_back(EndPoint);
						}
						PrevDist = NextDist;
						IsVisible = !IsVisible;
					}
					const UINT NumPoints = (UINT)NewPoints.size();
					if (NumPoints > 2 && Group.bLoop && FPathUtility::GetDistance(NewPoints[0].Position, NewPoints[NumPoints - 1].Position) < 1e-6f)
					{
						NewPoints[0].Flag = Point_Joint;
						NewPoints[NumPoints - 1].Flag = Point_Joint;
					}
					else
					{
						NewPoints[0].Flag = Point_Start;
						NewPoints[NumPoints - 1].Flag = Point_End;
					}

				}	break;
				case Stroke_Dot:
				{
					const float Length = Points[Group.End - 1].Distance - Points[Group.Begin].Distance;
					float Offset = Style->StrokeOffset;
					if (Offset < 0)
					{
						UINT NumOffsetRpt = (UINT)std::ceil(-Offset / Period);
						Offset = Period * NumOffsetRpt + Offset;
					}
					UINT NumRepeat = (UINT)std::ceil((Length + Offset) / Period);
					float PatternScalar = 1.0f;
					if (Style->StrokeAutoAlign)
					{
						PatternScalar = Length / (NumRepeat * Period);
						Offset *= PatternScalar;
						if (Offset > 0)
							NumRepeat += 1;
					}

					const UINT NumMaxClips = NumRepeat * (UINT)Pattern.size();
					float Distance = -Offset;
					for (UINT i = 0, PointInd = Group.Begin; i < NumMaxClips; i++)
					{
						Distance += Pattern[i % Pattern.size()] * PatternScalar;
						if (Distance > 0)
						{
							while (PointInd < Group.End && Points[PointInd].Distance < Distance)
								PointInd++;
							if (PointInd == Group.End)
								break;
							FPathPoint DotPoint = FPathPoint::Interplate(Points[PointInd - 1], Points[PointInd], Distance);
							DotPoint.Flag = Point_Single;
							NewPoints.push_back(DotPoint);
						}
					}

				} break;
				default:
					break;
				}

				if (NewPoints.empty())
					continue;

				const UINT NumPoints = (UINT)NewPoints.size();
				bool bHasPendingJoint = NewPoints[0].Flag == Point_Joint;
				UINT LineStartInd = 0;
				for (UINT i = 0; i < NumPoints; i++)
				{
					const auto& Point = NewPoints[i];
					switch (Point.Flag)
					{
					case Point_Single: 
						DrawCap(Point); 
						break;
					case Point_Start:
						DrawCap(Point);
						LineStartInd = i; 
						break;
					case Point_Joint:
						if (bHasPendingJoint)
						{
							UINT LastInd = i == 0 ? NumPoints - 1 : i - 1;
							DrawJoint(NewPoints[LastInd], Point);
							bHasPendingJoint = false;
							LineStartInd = i;
						}
						else
						{
							DrawLine(NewPoints.begin() + LineStartInd, NewPoints.begin() + i + 1);
							bHasPendingJoint = true;
						}
						break;
					case Point_End:
						DrawLine(NewPoints.begin() + LineStartInd, NewPoints.begin() + i + 1);
						DrawCap(Point);
						break;
					default:
						break;
					}
				}
			}
		}

		void FPath::Scissor(const FRectanglef& Rect)
		{
			for(auto& Poly : Polygons)
				Poly.WindowClip(Rect);
		}

		void FPath::Trianglute()
		{
			for (auto& Poly : Polygons)
				Poly.Traingulate();
		}

		void FPath::DrawLine(const std::vector<FPathPoint>::iterator& Begin, const std::vector<FPathPoint>::iterator& End)
		{
			ASSERT(Style && Style->PathWidth > 0.f);
			const float HalfWidth = Style->PathWidth * 0.5f;
			if (Begin == End)
				return;

			FPathPoly Poly;
			UINT NumVerts = 0;
			for (auto Iter = Begin; Iter != End; Iter++)
				NumVerts++;

			Poly.Vertices.resize(2 * NumVerts);
			for (UINT i = 0, j = 2 * NumVerts - 1; i < NumVerts; i++, j--)
			{
				const FPathPoint& Point = *(Begin + i);
				v3dxVector2 Right = FPathUtility::GetRight(Point.Forward);
				Poly.Vertices[j] = -Right * HalfWidth + Point.Position;
				Poly.Vertices[i] = Right * HalfWidth + Point.Position;
				if (i == 0)
					continue;
				Poly.Indices.push_back(j + 1);
				Poly.Indices.push_back(i - 1);
				Poly.Indices.push_back(i);
				Poly.Indices.push_back(j + 1);
				Poly.Indices.push_back(i);
				Poly.Indices.push_back(j);
			}

			if (Style->UvAlongPath)
			{
				Poly.UVs.resize(2 * NumVerts);
				for (UINT i = 0, j = 2 * NumVerts - 1; i < NumVerts; i++, j--)
				{
					const FPathPoint& Point = *(Begin + i);
					Poly.UVs[j] = v3dxVector2(Point.Distance, 0);
					Poly.UVs[i] = v3dxVector2(Point.Distance, Style->PathWidth);
				}
			}
			Polygons.push_back(std::move(Poly));
		}

		void FPath::DrawJoint(const FPathPoint& P0, const FPathPoint& P1)
		{
			ASSERT(Style && Style->PathWidth > 0.f);
			ASSERT(P0.Flag == Point_Joint && P1.Flag == Point_Joint);
			const float HalfWidth = Style->PathWidth * 0.5f;
			if (FPathUtility::GetDistance(P0.Forward * HalfWidth, P1.Forward * HalfWidth) < 1e-6f)
				return;

			switch (Style->JoinMode)
			{
			case Join_Miter: MiterJointInternal(P0, P1); break;
			case Join_Round: RoundJointInternal(P0, P1); break;
			case Join_Bevel: BevelJointInternal(P0, P1); break;
			default: break;
			}
		}

		void FPath::DrawCap(const FPathPoint& P0)
		{
			ASSERT(Style && Style->PathWidth > 0.f);
			ASSERT(P0.Flag == Point_Start || P0.Flag == Point_End || P0.Flag == Point_Single);
			switch (Style->CapMode)
			{
			case Cap_Butt: break;
			case Cap_Round: RoundCapInternal(P0); break;
			case Cap_Square: SquareCapInternal(P0); break;
			default: break;
			}
		}

		bool FPath::LineToInternal(const v3dxVector2& P0, const v3dxVector2& P1, float& Distance, FPathGroup& Group)
		{
			v3dxVector2 Direction;
			float Length;
			FPathUtility::SafeNormalize(P1 - P0, Direction, Length);
			if (Length < 1e-6f)
				return false;
			
			if (Group.Begin == Points.size())
				Points.push_back(FPathPoint( P0, Direction, Distance, Point_Start ));
			else
			{
				Points[Group.End - 1].Flag = Point_Joint;
				Points.push_back(FPathPoint( P0, Direction, Distance, Point_Joint ));
			}
			Distance += Length;
			Points.push_back(FPathPoint( P1, Direction, Distance, Point_End ));
			Group.End = (UINT)Points.size();
			return true;
		}

		bool FPath::ArcToInternal(const v3dxVector2& P0, const v3dxVector2& P1, float R, bool IsCCW, bool IsSmall, float& Distance, FPathGroup& Group)
		{
			v3dxVector2 N01;
			float L01;
			FPathUtility::SafeNormalize(P1 - P0, N01, L01);
			if (L01 < 1e-6f)
				return false;

			R = std::max(R, L01 * .5f);
			v3dxVector2 ToCenter = (IsCCW && !IsSmall) || (!IsCCW && IsSmall) ? FPathUtility::GetRight(N01) : -FPathUtility::GetRight(N01);
			v3dxVector2 Center = (P0 + P1) * .5f + std::sqrt(R * R - L01 * L01 * 0.25f) * ToCenter;

			v3dxVector2 Radial0 = (P0 - Center) / R;
			v3dxVector2 Radial1 = (P1 - Center) / R;

			v3dxVector2 Forward0 = -FPathUtility::GetRight(Radial0);
			v3dxVector2 Forward1 = -FPathUtility::GetRight(Radial1);
			if (!IsCCW)
			{
				Forward0 = -Forward0;
				Forward1 = -Forward1;
			}

			if (Group.Begin == Points.size())
				Points.push_back(FPathPoint( P0, Forward0, Distance, Point_Start ));
			else
			{
				Points[Group.End - 1].Flag = Point_Joint;
				Points.push_back(FPathPoint( P0, Forward0, Distance, Point_Joint ));
			}

#if FAST_ARC
			float Arc0, Arc1;
			UINT Index0 = GArcLUT.GetLUTIndex(Radial0, IsCCW, Arc0);
			UINT Index1 = GArcLUT.GetLUTIndex(Radial1, IsCCW, Arc1);
			if (Index0 == Index1 && ((IsCCW && Arc0 < Arc1) || (!IsCCW && Arc0 > Arc1)))
			{
				Distance += std::abs(Arc1 - Arc0) * R;
				Points.push_back(FPathPoint(P1, Forward1, Distance, Point_End));
			}
			else
			{
				Distance += Arc0 * R;
				UINT LastInd = Index0;
				while (true)
				{
					v3dxVector2 Forward;
					UINT NextInd;
					if (IsCCW)
					{
						Forward = -FPathUtility::GetRight(GArcLUT.LUT[LastInd]);
						NextInd = (LastInd + 1) % FArcLUT::NUM_FASTLUT;
					}
					else
					{
						Forward = FPathUtility::GetRight(GArcLUT.LUT[LastInd]);
						NextInd = (LastInd + FArcLUT::NUM_FASTLUT - 1) % FArcLUT::NUM_FASTLUT;
					}
					v3dxVector2 P = Center + GArcLUT.LUT[LastInd] * R;
					Points.push_back(FPathPoint(P, Forward, Distance, Point_None));
					if (NextInd == Index1)
						break;

					Distance += FArcLUT::DELTA * R;
					LastInd = NextInd;
				}
				Distance += (FArcLUT::DELTA - Arc1) * R;
				Points.push_back(FPathPoint(P1, Forward1, Distance, Point_End));
			}
#else
			const float Rad0 = std::atan2(Radial0.y, Radial0.x);
			const float Rad1 = std::atan2(Radial1.y, Radial1.x);
			const float Delta = IsCCW
				? (Rad0 < Rad1 ? Rad1 - Rad0 : FPathUtility::TWO_PI + Rad1 - Rad0)
				: (Rad0 > Rad1 ? Rad1 - Rad0 : -FPathUtility::TWO_PI + Rad1 - Rad0);
			const float AbsDelta = std::abs(Delta);
			const UINT NumSub = (const UINT)std::ceil(AbsDelta / FArcLUT::DELTA);
			const float Sub = Delta / NumSub;
			for (UINT i = 1; i < NumSub; i++)
			{
				float Rad = Rad0 + Sub * i;
				v3dxVector2 Radial = v3dxVector2(std::cos(Rad), std::sin(Rad));
				v3dxVector2 Forward = -FPathUtility::GetRight(Radial);
				if (!IsCCW)
					Forward = -Forward;
				v3dxVector2 P = Center + Radial * R;
				Points.push_back(FPathPoint(P, Forward, Distance + std::abs(Sub * i) * R, Point_None));
			}
			Distance += AbsDelta * R;
			Points.push_back(FPathPoint(P1, Forward1, Distance, Point_End));
#endif
			Group.End = (UINT)Points.size();
			return true;
		}

		void FPath::MiterJointInternal(const FPathPoint& P0, const FPathPoint& P1)
		{
			const float HalfWidth = Style->PathWidth * 0.5f;
			const float SqrSinHalf = (1 - FPathUtility::DotProduct(P0.Forward, P1.Forward)) * 0.5f;
			if (SqrSinHalf > 1 / std::pow(Style->MiterLimit, 2))
				return BevelJointInternal(P0, P1);

			v3dxVector2 Pos0 = P0.Position;
			v3dxVector2 Dir0 = P0.Forward;
			v3dxVector2 Dir1 = P1.Forward;
			const float CrossValue = FPathUtility::CrossProduct(Dir0 * HalfWidth, Dir1 * HalfWidth);
			if (std::abs(CrossValue) < 1e-6f)
				return;

			const float CosHalf = std::sqrtf(1 - SqrSinHalf);
			v3dxVector2 Right0 = FPathUtility::GetRight(Dir0);
			v3dxVector2 Right1 = FPathUtility::GetRight(Dir1);
			v3dxVector2 ClimaxDir = FPathUtility::Normalize(Right0 + Right1);

			FPathPoly Poly;
			Poly.Vertices.reserve(Poly.Vertices.size() + 4);
			if (CrossValue > 0)
			{
				Poly.UVs.reserve(Poly.UVs.size() + 4);
				Poly.Vertices.push_back(Pos0);
				Poly.Vertices.push_back(Pos0 + Right0 * HalfWidth);
				Poly.Vertices.push_back(Pos0 + ClimaxDir * HalfWidth / CosHalf);
				Poly.Vertices.push_back(Pos0 + Right1 * HalfWidth);
			}
			else
			{
				Poly.Vertices.push_back(Pos0);
				Poly.Vertices.push_back(Pos0 - Right1 * HalfWidth);
				Poly.Vertices.push_back(Pos0 - ClimaxDir * HalfWidth / CosHalf);
				Poly.Vertices.push_back(Pos0 - Right0 * HalfWidth);
			}

			if (Style->UvAlongPath)
			{
				Poly.UVs.reserve(Poly.UVs.size() + 4);
				auto UvOutline = v3dxVector2(P0.Distance, CrossValue > 0 ? Style->PathWidth : 0);
				Poly.UVs.push_back(v3dxVector2(P0.Distance, HalfWidth));
				Poly.UVs.push_back(UvOutline);
				Poly.UVs.push_back(UvOutline);
				Poly.UVs.push_back(UvOutline);
			}

			Poly.Indices.reserve(Poly.Indices.size() + 6);
			Poly.Indices.push_back(0);
			Poly.Indices.push_back(1);
			Poly.Indices.push_back(2);
			Poly.Indices.push_back(0);
			Poly.Indices.push_back(2);
			Poly.Indices.push_back(3);
			Polygons.push_back(std::move(Poly));
		}

		void FPath::RoundJointInternal(const FPathPoint& P0, const FPathPoint& P1)
		{
			const float HalfWidth = Style->PathWidth * 0.5f;
			v3dxVector2 Pos0 = P0.Position;
			v3dxVector2 Dir0 = P0.Forward;
			v3dxVector2 Dir1 = P1.Forward;
			if (FPathUtility::GetDistance(Dir0 * HalfWidth, Dir1 * HalfWidth) < 1e-6f)
				return;

			const bool IsCCW = FPathUtility::CrossProduct(Dir0 * HalfWidth, Dir1 * HalfWidth) > 0;
			v3dxVector2 Radial0 = FPathUtility::GetRight(Dir0);
			v3dxVector2 Radial1 = FPathUtility::GetRight(Dir1);
			if (!IsCCW)
			{
				Radial0 = -Radial1;
				Radial1 = -Radial0;
			}

			FPathPoly Poly;
			Poly.Vertices.push_back(Pos0);
			Poly.Vertices.push_back(Pos0 + Radial0 * HalfWidth);
#if FAST_ARC
			UINT Index0 = GArcLUT.GetLUTIndex(Radial0);
			UINT Index1 = GArcLUT.GetLUTIndex(Radial1);
			if (Index0 != Index1)
			{
				UINT LastInd = Index0;
				while (true)
				{
					UINT NextInd = (LastInd + 1) % FArcLUT::NUM_FASTLUT;
					if (NextInd == Index1)
						break;
					LastInd = NextInd;
					Poly.Vertices.push_back(Pos0 + GArcLUT.LUT[NextInd] * HalfWidth);
				}
			}
#else
			const float Rad0 = std::atan2(Radial0.y, Radial0.x);
			const float Rad1 = std::atan2(Radial1.y, Radial1.x);
			const float Delta = IsCCW
				? (Rad0 < Rad1 ? Rad1 - Rad0 : FPathUtility::TWO_PI + Rad1 - Rad0)
				: (Rad0 > Rad1 ? Rad1 - Rad0 : -FPathUtility::TWO_PI + Rad1 - Rad0);
			const UINT NumSub = (const UINT)std::ceil(std::abs(Delta) / FArcLUT::DELTA);
			const float Sub = Delta / NumSub;
			for (UINT i = 1; i < NumSub; i++)
			{
				float Rad = Rad0 + Sub * i;
				v3dxVector2 Radial = v3dxVector2(std::cos(Rad), std::sin(Rad));
				Poly.Vertices.push_back(Pos0 + Radial * HalfWidth);
			}
#endif
			Poly.Vertices.push_back(Pos0 + Radial1 * HalfWidth);

			const UINT NumVerts = Poly.GetSize();
			for (UINT i = 2; i < NumVerts; i++)
			{
				Poly.Indices.push_back(0);
				Poly.Indices.push_back(i - 1);
				Poly.Indices.push_back(i);
			}

			if (Style->UvAlongPath)
			{
				const UINT NumVerts = Poly.GetSize();
				Poly.UVs.push_back(v3dxVector2(P0.Distance, HalfWidth));
				v3dxVector2 OutlineUV(P0.Distance, IsCCW ? Style->PathWidth : 0.f);
				for (UINT i = 1; i < NumVerts; i++)
					Poly.UVs.push_back(OutlineUV);
			}
			Polygons.push_back(std::move(Poly));
		}

		void FPath::BevelJointInternal(const FPathPoint& P0, const FPathPoint& P1)
		{
			const float HalfWidth = Style->PathWidth * 0.5f;

			v3dxVector2 Pos0 = P0.Position;
			v3dxVector2 Dir0 = P0.Forward;
			v3dxVector2 Dir1 = P1.Forward;
			const float CrossValue = FPathUtility::CrossProduct(Dir0 * HalfWidth, Dir1 * HalfWidth);
			if (std::abs(CrossValue) < 1e-6f)
				return;

			FPathPoly Poly;
			if (CrossValue > 0)
			{
				Poly.Vertices.push_back(Pos0);
				Poly.Vertices.push_back(Pos0 + FPathUtility::GetRight(Dir0) * HalfWidth);
				Poly.Vertices.push_back(Pos0 + FPathUtility::GetRight(Dir1) * HalfWidth);
			}
			else
			{
				Poly.Vertices.push_back(Pos0);
				Poly.Vertices.push_back(Pos0 - FPathUtility::GetRight(Dir1) * HalfWidth);
				Poly.Vertices.push_back(Pos0 - FPathUtility::GetRight(Dir0) * HalfWidth);
			}

			if (Style->UvAlongPath)
			{
				v3dxVector2 UvOutline = v3dxVector2(P0.Distance, CrossValue > 0 ? Style->PathWidth : 0);
				Poly.UVs.push_back(v3dxVector2(P0.Distance, HalfWidth));
				Poly.UVs.push_back(UvOutline);
				Poly.UVs.push_back(UvOutline);
			}

			Poly.Indices.push_back(0);
			Poly.Indices.push_back(1);
			Poly.Indices.push_back(2);
			Polygons.push_back(std::move(Poly));
		}

		void FPath::RoundCapInternal(const FPathPoint& P0)
		{
			const float HalfWidth = Style->PathWidth * 0.5f;
			const bool UvAlongPath = Style->UvAlongPath;
			v3dxVector2 Position = P0.Position;
			v3dxVector2 Direction = P0.Forward;
			v3dxVector2 Right = FPathUtility::GetRight(Direction);

			auto GetUV = [=](v3dxVector2 InExt)
			{
				return v3dxVector2(
					FPathUtility::DotProduct(Direction, InExt) + P0.Distance,
					FPathUtility::DotProduct(Right, InExt) + HalfWidth);
			};

			FPathPoly Poly;
			switch (P0.Flag)
			{
			case Point_Single:
			{
				for (UINT i = 0; i < FArcLUT::NUM_FASTLUT; i++)
				{
					v3dxVector2 Extent = GArcLUT.LUT[i] * HalfWidth;
					Poly.Vertices.push_back(Position + Extent);
					if (UvAlongPath)
					{
						Poly.UVs.push_back(GetUV(Extent));
					}
				}

			} break;
			case Point_Start:
			case Point_End:
			{
				if (P0.Flag == Point_Start)
					Right = -Right;
				v3dxVector2 Extent = Right * HalfWidth;
				Poly.Vertices.push_back(Position + Extent);
				if (UvAlongPath)
				{
					Poly.UVs.push_back(GetUV(Extent));
				}
#if FAST_ARC
				UINT Index = GArcLUT.GetLUTIndex(Right, true);
				for (UINT i = 0; i < FArcLUT::NUM_FASTLUT / 2; i++)
				{
					UINT LUTInd = (Index + i) % FArcLUT::NUM_FASTLUT;
					Extent = GArcLUT.LUT[LUTInd] * HalfWidth;
					Poly.Vertices.push_back(Position + Extent);
					if (UvAlongPath)
					{
						Poly.UVs.push_back(GetUV(Extent));
					}
				}
#else
				const float sRad = std::atan2(Right.y, Right.x);
				const UINT NumSub = (const UINT)std::ceil(FPathUtility::PI / FArcLUT::DELTA);
				const float Sub = FPathUtility::PI / NumSub;
				for (UINT i = 1; i < NumSub; i++)
				{
					float Rad = sRad + Sub * i;
					v3dxVector2 Radial = v3dxVector2(std::cos(Rad), std::sin(Rad));
					Poly.Vertices.push_back(Position + Radial * HalfWidth);
					if (UvAlongPath)
					{
						Poly.UVs.push_back(GetUV(Extent));
					}
				}
#endif
				Extent = -Right * HalfWidth;
				Poly.Vertices.push_back(Position + Extent);
				if (UvAlongPath)
				{
					Poly.UVs.push_back(GetUV(Extent));
				}

			} break;
			default:
				break;
			}
			Polygons.push_back(std::move(Poly));
		}

		void FPath::SquareCapInternal(const FPathPoint& P0)
		{
			const float HalfWidth = Style->PathWidth * 0.5f;
			v3dxVector2 Position = P0.Position;
			v3dxVector2 Direction = P0.Forward;
			v3dxVector2 Right = FPathUtility::GetRight(P0.Forward);

			FPathPoly Poly;
			switch (P0.Flag)
			{
			case Point_Single:
			{
				Poly.Vertices.push_back(Position - (Right - Direction) * HalfWidth);
				Poly.Vertices.push_back(Position + (Right - Direction) * HalfWidth);
				Poly.Vertices.push_back(Position + (Right + Direction) * HalfWidth);
				Poly.Vertices.push_back(Position + (-Right + Direction) * HalfWidth);

				if (Style->UvAlongPath)
				{
					Poly.UVs.push_back(v3dxVector2(P0.Distance - HalfWidth, 0));
					Poly.UVs.push_back(v3dxVector2(P0.Distance - HalfWidth, Style->PathWidth));
					Poly.UVs.push_back(v3dxVector2(P0.Distance + HalfWidth, Style->PathWidth));
					Poly.UVs.push_back(v3dxVector2(P0.Distance + HalfWidth, 0));
				}

			} break;
			case Point_Start:
			{
				Poly.Vertices.push_back(Position + (-Right - Direction) * HalfWidth);
				Poly.Vertices.push_back(Position + (Right - Direction) * HalfWidth);
				Poly.Vertices.push_back(Position + Right  * HalfWidth);
				Poly.Vertices.push_back(Position - Right * HalfWidth);

				if (Style->UvAlongPath)
				{
					Poly.UVs.push_back(v3dxVector2(P0.Distance - HalfWidth, 0));
					Poly.UVs.push_back(v3dxVector2(P0.Distance - HalfWidth, Style->PathWidth));
					Poly.UVs.push_back(v3dxVector2(P0.Distance, Style->PathWidth));
					Poly.UVs.push_back(v3dxVector2(P0.Distance, 0));
				}

			} break;
			case Point_End:
			{
				Poly.Vertices.push_back(Position - Right * HalfWidth);
				Poly.Vertices.push_back(Position + Right * HalfWidth);
				Poly.Vertices.push_back(Position + (Right + Direction) * HalfWidth);
				Poly.Vertices.push_back(Position + (-Right + Direction) * HalfWidth);

				if (Style->UvAlongPath)
				{
					Poly.UVs.push_back(v3dxVector2(P0.Distance, 0));
					Poly.UVs.push_back(v3dxVector2(P0.Distance, Style->PathWidth));
					Poly.UVs.push_back(v3dxVector2(P0.Distance + HalfWidth, Style->PathWidth));
					Poly.UVs.push_back(v3dxVector2(P0.Distance + HalfWidth, 0));
				}

			} break;
			default:
				break;
			}
			Polygons.push_back(std::move(Poly));
		}

		void FPath::UpdateBounds()
		{
			Bounds = FRectanglef(0, 0, 0, 0);
			if (!Polygons.empty())
			{
				bool bInit = false;
				for (auto Poly : Polygons)
				{
					const auto& PolyBounds = Poly.GetBounds();
					if (PolyBounds.IsValid())
					{
						if (bInit)
							Bounds = FRectanglef::Or(Bounds, PolyBounds);
						else
						{
							Bounds = PolyBounds;
							bInit = true;
						}
					}
				}
			}
			else if (!Points.empty())
			{
				float MinX = std::numeric_limits<float>::max();
				float MaxX = std::numeric_limits<float>::lowest();
				float MinY = MinX;
				float MaxY = MaxX;
				for (const auto& Point : Points)
				{
					const auto& Pos = Point.Position;
					MinX = std::min(MinX, Pos.x);
					MaxX = std::max(MaxX, Pos.x);
					MinY = std::min(MinY, Pos.y);
					MaxY = std::max(MaxY, Pos.y);
				}
				Bounds = FRectanglef(MinX, MinY, MaxX - MinX, MaxY - MinY);
			}
		}

		v3dxVector2 FPath::RemapUV(const FRectanglef& InBounds, const FRectanglef& InBrush, const v3dxVector4& InTiling, const v3dxVector2& InUV) const
		{
			ASSERT(Style);
			ASSERT(InBounds.IsValid());
			v3dxVector2 UV = InUV;
			switch (Style->PaintMode)
			{
			case Paint_Center:
			{
				v3dxVector2 Scalar = v3dxVector2(
					InBrush.Width >= 0.f ? 1.f : -1.f,
					InBrush.Height >= 0.f ? 1.f : -1.f);
				UV = (UV - InBounds.GetCenter()) * Scalar + InBrush.GetCenter();
			
			} break;
			case Paint_Fit:
			{
				float Scalar = std::max(
					InBrush.Width / InBounds.Width,
					InBrush.Height / InBounds.Height);
				UV = (UV - InBounds.GetCenter()) * Scalar + InBrush.GetCenter();

			} break;
			case Paint_Fill:
			{
				float Scalar = std::min(
					InBrush.Width / InBounds.Width,
					InBrush.Height / InBounds.Height);
				UV = (UV - InBounds.GetCenter()) * Scalar + InBrush.GetCenter();
			
			} break;
			case Paint_Stretch:
			{
				v3dxVector2 Scalar = v3dxVector2(
					InBrush.Width / InBounds.Width,
					InBrush.Height / InBounds.Height);
				UV = (UV - InBounds.GetCenter()) * Scalar + InBrush.GetCenter();
			
			} break;
			case Paint_Free:
			default:
				break;
			}

			UV = v3dxVector2(
				UV.x * InTiling.x - InTiling.z,
				UV.y * InTiling.y - InTiling.w);
			return UV;
		}
	}
}

NS_END