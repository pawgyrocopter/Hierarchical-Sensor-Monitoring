#pragma once

#include "msclr/auto_gcroot.h"

using namespace HSMDataCollector::PublicInterface;

namespace hsm_wrapper
{
	template<class T>
	struct BarSensorType;

	template<>
	struct BarSensorType<int>
	{
		using type = IIntBarSensor^;
	};

	template<>
	struct BarSensorType<double>
	{
		using type = IDoubleBarSensor^;
	};

	template<class T>
	class HSMBarSensorImpl
	{
	public:
		HSMBarSensorImpl(typename BarSensorType<T>::type sensor);
		~HSMBarSensorImpl() = default;
		HSMBarSensorImpl() = delete;
		HSMBarSensorImpl(const HSMBarSensorImpl&) = delete;
		HSMBarSensorImpl(HSMBarSensorImpl&&) = delete;
		HSMBarSensorImpl& operator=(const HSMBarSensorImpl&) = delete;
		HSMBarSensorImpl& operator=(HSMBarSensorImpl&&) = delete;

		void AddValue(T value);
	private:
		msclr::auto_gcroot<typename BarSensorType<T>::type> sensor;
	};
}