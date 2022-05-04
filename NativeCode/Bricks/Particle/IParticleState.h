#pragma once
#include "../../Base/IUnknown.h"
#include "../../Base/debug//vfxdebug.h"
#include "../../Math/v3dxVector3.h"
#include "../../Math/v3dxQuaternion.h"
#include "../../RHI/IRenderResource.h"
#include "../../RHI/Utility/IMeshPrimitives.h"

NS_BEGIN

struct TR_CLASS(SV_LayoutStruct = 8)
	IBaseParticleState
{
	UINT			Flags;
	float			Life;

	bool IsDeath() const {
		return (Life <= 0);
	}
};

struct TR_CLASS(SV_LayoutStruct = 8)
	FVarAttribute
{
	VNameString		Name;
	EShaderVarType	Type;
	UINT			Offset;
};

class TR_CLASS()
	IParticleSystemAttribute
{
	std::vector<FVarAttribute>		NamedAttributes;

	void AddAttribute(const char* name, EShaderVarType type)
	{
		FVarAttribute tmp;
		tmp.Name = name;
		tmp.Type = type;
		NamedAttributes.push_back(tmp);
	}
	void BuildAttributes(UINT AlignSize = 16);

	int FindAttribute(const char* name) const
	{
		for (int i = 0; i < (int)NamedAttributes.size(); i++)
		{
			if (NamedAttributes[i].Name == name)
				return i;
		}
		return -1;
	}
	FVarAttribute* GetAttributeAddress(int index)
	{
		return &NamedAttributes[index];
	}
};

class IEmitter;
class IParticlePool
{
public:
	UINT					mDataStride;
	std::vector<BYTE>		mParticleArray;
	std::vector<UINT>		mFreeParticles;
	std::vector<UINT>*		mCurAlives;
	std::vector<UINT>*		mBackendAlives;
	std::vector<UINT>		mLivedParticles[2];
	bool					mChanged;
public:
	IParticlePool()
	{
		mDataStride = sizeof(float);
	}
	bool InitPool(UINT dataStride, UINT maxNum);
	UINT Alloc(IEmitter* pEmitter, UINT num, UINT flags, float life);
	bool IsChanged() const {
		return mChanged;
	}
	void Recycle(IEmitter* pEmitter);

	inline BYTE* GetParticleAddress() {
		return mParticleArray.data();
	}
	inline UINT* GetCurrentAliveAddress() {
		return mCurAlives->data();
	}
	inline UINT* GetBackendAliveAddress() {
		return mBackendAlives->data();
	}
	inline IBaseParticleState* GetParticle(UINT index) {
		return (IBaseParticleState*)&mParticleArray[(size_t)index * mDataStride];
	}
	inline std::vector<UINT>* GetCurAlives() {
		return mCurAlives;
	}
	inline UINT GetLiveNumber() const{
		return (UINT)mCurAlives->size();
	}
	inline UINT GetBackendNumber() const {
		return (UINT)mBackendAlives->size();
	}
	inline IBaseParticleState* GetLiveParticle(UINT index)
	{
		auto addr = (*mCurAlives)[index];
		return (IBaseParticleState*)&mParticleArray[addr * mDataStride];
	}
};

NS_END